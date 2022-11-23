using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GrainInterfaces
{
    public interface ILargeDataGrain : Orleans.IGrainWithStringKey
    {
        Task<string> SayHello(string greeting);
    }
}
