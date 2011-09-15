// http://dotnet.org.za/hiltong/pages/Windows-Communication-Foundation-_2800_Indigo_2900_-Salutation-World-Tutorial.aspx
using System.ServiceModel;

namespace YepITWorks.ServiceHostHelper.Test.Services
{
    [ServiceBehavior(InstanceContextMode = InstanceContextMode.PerCall)]
    public class HelloWorldService : IHelloWorldService
    {
        public HelloWorld Salutate(string name)
        {
            return new HelloWorld {Salutation = "Hello, " + name + "!"};
        }
    }
}
