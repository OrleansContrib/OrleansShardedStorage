namespace GrainInterfaces
{
    public interface ISmallDataGrain : Orleans.IGrainWithIntegerKey
    {
        Task<string> SayHello(string greeting);
    }
}
