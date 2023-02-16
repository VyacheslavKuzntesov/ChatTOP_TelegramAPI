using System.Collections.Concurrent;

internal enum State
{
    None
}


internal class UserMessageDataState
{
    public ConcurrentQueue<DateTime> messageDateTimes { get; set; }

    public UserMessageDataState()
    {
        messageDateTimes = new ConcurrentQueue<DateTime>();
    }
}
