//
// Copyright (c) ZeroC, Inc. All rights reserved.
//
using System;

namespace ZeroC.Ice
{
    // The partial classes below extend the partial exception classes generated by the Slice compiler.

    public partial class DispatchException
    {
        public DispatchException(Current current)
            : this(current.Identity, current.Facet, current.Operation)
        {
        }

        protected override string DefaultMessage =>
            Facet.Length == 0 ? $"failed to dispatch request for operation `{Operation}' on Ice object `{Identity}'" :
            $"failed to dispatch request for operation `{Operation}' on Ice object `{Identity}' with facet `{Facet}'";
    }

    public partial class ObjectNotExistException
    {
        public ObjectNotExistException(Current current)
            : base(current)
        {
        }

        protected override string DefaultMessage =>
            $"could not find servant for Ice object `{Identity}'" + (Facet.Length > 0 ? $" with facet `{Facet}'" : "") +
            $" while attempting to dispatch operation `{Operation}'";
    }

    public partial class OperationNotExistException
    {
        public OperationNotExistException(Current current)
            : base(current)
        {
        }

        protected override string DefaultMessage =>
            $"could not find operation `{Operation}' for Ice object `{Identity}'" +
            (Facet.Length > 0 ? $" with facet `{Facet}'" : "");
    }

    public partial class UnhandledException
    {
        public UnhandledException(Identity identity, string facet, string operation, Exception innerException)
            : base(CustomMessage(identity, facet, operation, innerException),
                   identity,
                   facet,
                   operation,
                   innerException)
        {
        }

        private static string CustomMessage(Identity identity, string facet, string operation, Exception innerException)
        {
            string message = $"unhandled exception while dispatching `{operation}' on Ice object `{identity}'";
            if (facet.Length > 0)
            {
                message += $" with facet `{facet}'";
            }
#if DEBUG
            message += $":\n{innerException}\n---";
#else
            // The stack trace of the inner exception can include sensitive information we don't want to send
            // "over the wire" in non-debug builds.
            message += $":\n{innerException.Message}";
#endif
            return message;
        }
    }
}
