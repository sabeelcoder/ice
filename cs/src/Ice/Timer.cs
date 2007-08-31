// **********************************************************************
//
// Copyright (c) 2003-2007 ZeroC, Inc. All rights reserved.
//
// This copy of Ice is licensed to you under the terms described in the
// ICE_LICENSE file included in this distribution.
//
// **********************************************************************

namespace IceInternal
{
    using System.Diagnostics;
    using System.Threading;
    using System.Collections.Generic;

    public interface TimerTask
    {
        void run();
    };

    public sealed class Timer
    {
        public void destroy()
        {
            lock(this)
            {
                if(_instance == null)
                {
                    return;
                }

                _instance = null;
                System.Threading.Monitor.Pulse(this);
            
                _tokens.Clear();
                _tasks.Clear();
            }

            _thread.Join();
        }


        public void schedule(TimerTask task, long delay)
        {
            lock(this)
            {
                if(_instance == null)
                {
                    return;
                }

                Token token = new Token(System.DateTime.Now.Ticks / 10000 + delay, ++_tokenId, 0, task);

                try
                {
                    _tasks.Add(task, token);
                    _tokens.Add(token, null);
                }
                catch(System.ArgumentException)
                {
                    Debug.Assert(false);
                }
            
                if(token.scheduledTime < _wakeUpTime)
                {
                    System.Threading.Monitor.Pulse(this);
                }
            }
        }

        public void scheduleRepeated(TimerTask task, long period)
        {
            lock(this)
            {
                if(_instance == null)
                {
                    return;
                }

                Token token = new Token(System.DateTime.Now.Ticks / 10000 + period, ++_tokenId, period, task);

                try
                {
                    _tasks.Add(task, token);
                    _tokens.Add(token, null);
                }
                catch(System.ArgumentException)
                {
                    Debug.Assert(false);
                }

                if(token.scheduledTime < _wakeUpTime)
                {
                    System.Threading.Monitor.Pulse(this);
                }
            }
        } 
        
        public bool cancel(TimerTask task)
        {
            lock(this)
            {
                if(_instance == null)
                {
                    return false;
                }

                Token token;
                if(!_tasks.TryGetValue(task, out token))
                {
                    return false;
                }
                _tasks.Remove(task);
                _tokens.Remove(token);
                return true;
            }
        }

        //
        // Only for use by Instance.
        //
        internal Timer(IceInternal.Instance instance)
        {
            _instance = instance;
            
            string threadName = _instance.initializationData().properties.getProperty("Ice.ProgramName");
            if(threadName.Length > 0)
            {
                threadName += "-";
            }
            
            _thread = new Thread(new ThreadStart(Run));
            _thread.IsBackground = true;
            _thread.Name = threadName + "Ice.Timer";
            _thread.Start();
        }

        public void Run()
        {
            Token token = null;
            while(true)
            {
                lock(this)
                {
                    if(_instance != null)
                    {
                        //
                        // If the task we just ran is a repeated task, schedule it
                        // again for executation if it wasn't canceled.
                        //
                        if(token != null && token.delay > 0)
                        {
                            if(_tasks.ContainsKey(token.task))
                            {
                                token.scheduledTime = System.DateTime.Now.Ticks / 10000 + token.delay;
                                _tokens.Add(token, null);
                            }
                        }
                    }
                    token = null;

                    if(_instance == null)
                    {
                        break;
                    }

                    if(_tokens.Count == 0)
                    {
                        _wakeUpTime = System.Int64.MaxValue;
                        System.Threading.Monitor.Wait(this);
                    }
            
                    if(_instance == null)
                    {
                        break;
                    }
                
                    while(_tokens.Count > 0 && _instance != null)
                    {
                        long now = System.DateTime.Now.Ticks / 10000;

                        Token first = null;
                        foreach(Token t in _tokens.Keys)
                        {
                            first = t;
                            break;
                        }
                        Debug.Assert(first != null);

                        if(first.scheduledTime <= now)
                        {
                            _tokens.Remove(first);
                            token = first;
                            if(token.delay == 0)
                            {
                                _tasks.Remove(token.task);
                            }
                            break;
                        }
                    
                        _wakeUpTime = first.scheduledTime;
                        System.Threading.Monitor.Wait(this, (int)(first.scheduledTime - now));
                    }
                
                    if(_instance == null)
                    {
                        break;
                    }
                }

                if(token != null)
                {
                    try
                    {
                        token.task.run();
                    }
                    catch(System.Exception ex)
                    {
                        lock(this)
                        {
                            if(_instance != null)
                            {
                                string s = "unexpected exception from task run method in timer thread:\n" + ex;
                                _instance.initializationData().logger.error(s);
                            }
                        }
                    } 
                }
            }
        }

        private class Token : System.IComparable
        {
            public
            Token(long scheduledTime, int id, long delay, TimerTask task)
            {
                this.scheduledTime = scheduledTime;
                this.id = id;
                this.delay = delay;
                this.task = task;
            }

            public int CompareTo(object o)
            {
                //
                // Token are sorted by scheduled time and token id.
                //
                Token r = (Token)o;
                if(scheduledTime < r.scheduledTime)
                {
                    return -1;
                }
                else if(scheduledTime > r.scheduledTime)
                {
                    return 1;
                }

                if(id < r.id)
                {
                    return -1;
                }
                else if(id > r.id)
                {
                    return 1;
                }
            
                return 0;
            }

            public long scheduledTime;
            public int id; // Since we can't compare references, we need to use another id.
            public long delay;
            public TimerTask task;
        }

        private IDictionary<Token, object> _tokens = new SortedDictionary<Token, object>();
        private IDictionary<TimerTask, Token> _tasks = new Dictionary<TimerTask, Token>();
        private Instance _instance;
        private long _wakeUpTime = System.Int64.MaxValue;
        private int _tokenId = 0;
        private Thread _thread;
   }
}