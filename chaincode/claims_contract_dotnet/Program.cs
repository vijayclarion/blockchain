using System;
using System.Threading.Tasks;
using Hyperledger.Fabric.Shim;

namespace ClaimsContract
{
    class Program
    {
        public static async Task Main(string[] args)
        {
            try
            {
                // This starts the chaincode and registers it with the peer
                await Chaincode.Start(args, new ClaimsContract());
            }
            catch (Exception e)
            {
                Console.WriteLine("Error starting chaincode: " + e.Message);
            }
        }
    }
}
