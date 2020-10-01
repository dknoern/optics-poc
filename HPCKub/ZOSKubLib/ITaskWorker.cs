namespace ZOSKubLib
{
    public interface ITaskWorker
    {
        byte[] OnTask(byte[] input);
    }
}