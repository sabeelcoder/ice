// **********************************************************************
//
// Copyright (c) 2001
// MutableRealms, Inc.
// Huntsville, AL, USA
//
// All Rights Reserved
//
// **********************************************************************

package Ice;

class PropertiesI implements Properties
{
    public synchronized String
    getProperty(String key)
    {
        String result = (String)_properties.get(key);
        if (result == null)
        {
            result = System.getProperty(key);
        }
        if (result == null)
        {
            result = "";
        }
        return result;
    }

    public synchronized String
    getPropertyWithDefault(String key, String value)
    {
        String result = (String)_properties.get(key);
        if (result == null)
        {
            result = System.getProperty(key);
        }
        if (result == null)
        {
            result = value;
        }
        return result;
    }

    public int
    getPropertyAsInt(String key)
    {
	return getPropertyAsIntWithDefault(key, 0);
    }

    public synchronized int
    getPropertyAsIntWithDefault(String key, int value)
    {
        String result = (String)_properties.get(key);
        if (result == null)
        {
            result = System.getProperty(key);
        }
        if (result == null)
        {
            return value;
        }

	try
	{
	    return Integer.parseInt(result);
	}
	catch (NumberFormatException ex)
	{
	    return 0;
	}
    }

    public synchronized String[]
    getProperties(String prefix)
    {
        java.util.ArrayList l = new java.util.ArrayList();
        java.util.Iterator p = _properties.entrySet().iterator();
        while (p.hasNext())
        {
            java.util.Map.Entry entry = (java.util.Map.Entry)p.next();
            String key = (String)entry.getKey();
            if (prefix.length() == 0 || key.startsWith(prefix))
            {
                l.add(key);
                l.add(entry.getValue());
            }
        }

        String[] result = new String[l.size()];
        l.toArray(result);
        return result;
    }

    public synchronized void
    setProperty(String key, String value)
    {
        _properties.put(key, value);
    }

    public synchronized String[]
    getCommandLineOptions()
    {
        String[] result = new String[_properties.size()];
        java.util.Iterator p = _properties.entrySet().iterator();
        int i = 0;
        while (p.hasNext())
        {
            java.util.Map.Entry entry = (java.util.Map.Entry)p.next();
            result[i++] = "--" + entry.getKey() + "=" + entry.getValue();
        }
        assert(i == result.length);
        return result;
    }

    public synchronized String[]
    parseCommandLineOptions(String prefix, String[] options)
    {
        java.util.ArrayList result = new java.util.ArrayList();
        for (int i = 0; i < options.length; i++)
        {
            String opt = options[i];
            if (opt.startsWith("--" + prefix + "."))
            {
                if (opt.indexOf('=') == -1)
                {
                    opt += "=1";
                }

                parseLine(opt.substring(2));
            }
            else
            {
                result.add(opt);
            }
        }
        String[] arr = new String[result.size()];
        result.toArray(arr);
        return arr;
    }

    public synchronized void
    load(String file)
    {
        try
        {
            java.io.FileReader fr = new java.io.FileReader(file);
            java.io.BufferedReader br = new java.io.BufferedReader(fr);
            parse(br);
        }
        catch (java.io.IOException ex)
        {
            SystemException se = new SystemException();
            se.initCause(ex); // Exception chaining
            throw se;
        }
    }

    public synchronized Properties
    _clone()
    {
        PropertiesI p = new PropertiesI(new String[0]);
        p._properties.putAll(_properties);
        return p;
    }

    PropertiesI()
    {
    }

    PropertiesI(String[] args)
    {
        for (int i = 0; i < args.length; i++)
        {
            if (args[i].startsWith("--Ice.Config"))
            {
                String line = args[i];
                if (line.indexOf('=') == -1)
                {
                    line += "=1";
                }
                parseLine(line.substring(2));
            }
        }

        String file = getProperty("Ice.Config");

        if (file.equals("1"))
        {
            file = "";
        }

        if (file.length() > 0)
        {
            load(file);
        }

        setProperty("Ice.Config", file);
    }

    PropertiesI(StringSeqHolder args)
    {
        for (int i = 0; i < args.value.length; i++)
        {
            if (args.value[i].startsWith("--Ice.Config"))
            {
                String line = args.value[i];
                if (line.indexOf('=') == -1)
                {
                    line += "=1";
                }
                parseLine(line.substring(2));
                String[] arr = new String[args.value.length - 1];
                System.arraycopy(args.value, 0, arr, 0, i);
                if (i < args.value.length - 1)
                {
                    System.arraycopy(args.value, i + 1, arr, i, args.value.length - i - 1);
                }
                args.value = arr;
            }
        }

        String file = getProperty("Ice.Config");

        if (file.equals("1"))
        {
            file = "";
        }

        if (file.length() > 0)
        {
            load(file);
        }

        setProperty("Ice.Config", file);
    }

    private void
    parse(java.io.BufferedReader in)
    {
        try
        {
            String line;
            while ((line = in.readLine()) != null)
            {
                parseLine(line);
            }
        }
        catch (java.io.IOException ex)
        {
            SystemException se = new SystemException();
            se.initCause(ex); // Exception chaining
            throw se;
        }
    }

    private void
    parseLine(String line)
    {
        String s = line;

        int hash = s.indexOf('#');
        if (hash == 0)
        {
            return; // ignore comment lines
        }
        else if (hash != -1)
        {
            s = s.substring(0, hash);
        }

        s = s.trim();

        final char[] arr = s.toCharArray();
        int end = -1;
        for (int i = 0; i < arr.length; i++)
        {
            if (arr[i] == ' ' || arr[i] == '\t' || arr[i] == '\r' || arr[i] == '\n' || arr[i] == '=')
            {
                end = i;
                break;
            }
        }
        if (end == -1)
        {
            return;
        }

        String key = s.substring(0, end);

        end = s.indexOf('=', end);
        if (end == -1)
        {
            return;
        }
        ++end;

        String value = "";
        if (end < s.length())
        {
            value = s.substring(end).trim();
        }

        setProperty(key, value);
    }

    private java.util.HashMap _properties = new java.util.HashMap();
    private static java.util.HashSet _argumentPrefixes = new java.util.HashSet();
}
