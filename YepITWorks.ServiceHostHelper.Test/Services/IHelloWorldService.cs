using System.ServiceModel;

namespace YepITWorks.ServiceHostHelper.Test.Services
{
    [ServiceContract]
    public interface IHelloWorldService
    {
        [OperationContract] //(ProtectionLevel = ProtectionLevel.EncryptAndSign)]
        HelloWorld Salutate(string name);
    }
}