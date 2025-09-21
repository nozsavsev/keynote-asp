namespace keynote_asp.Exceptions
{
    public class KeynoteException : Exception
    {
        public WrResponseStatus Status { get; }

        public KeynoteException(WrResponseStatus status, string message = "") : base(message)
        {
            Status = status;
        }
    }
}