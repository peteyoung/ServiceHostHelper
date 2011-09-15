using System;
using System.Runtime.Serialization;

namespace YepITWorks.ServiceHostHelper.Test.Services
{
    [DataContract]
    public class HelloWorld
    {
        [DataMember] public virtual String Salutation { get; set; }
    }
}
