using System;
using System.ServiceModel;
using System.ServiceModel.Channels;
using NUnit.Framework;
using FluentAssertions;
using YepITWorks.ServiceHostHelper.Test.Services;

namespace YepITWorks.ServiceHostHelper.Test
{
    [TestFixture]
    public class ServiceHostHelperTest
    {
        [Test]
        public void Should_create_ServiceHost()
        {
            using (var helper = new ServiceHostHelper<HelloWorldService, IHelloWorldService>())
            {
                var serviceHost = helper.CreateNewServiceHost();
                serviceHost.Should().NotBeNull();
                serviceHost.Description.ServiceType.Should().Be(typeof(HelloWorldService));

                // NOTE: This will fail. Compare and contrast with the similar line above
                //serviceHost.Description.ServiceType.Should().BeOfType<HelloWorldService>();
            }
        }

        [Test]
        public void Should_throw_argumentnullexception_when_constructor_is_passed_a_null_binding()
        {
            Assert.Throws<ArgumentNullException>(
                () => new ServiceHostHelper<HelloWorldService, IHelloWorldService>(null, "foo://bar/baz/"));
        }

        [Test]
        public void Should_throw_argumentnullexception_when_constructor_is_passed_a_null_address()
        {
            var binding = new NetTcpBinding();

            Assert.Throws<ArgumentNullException>(
                () => new ServiceHostHelper<HelloWorldService, IHelloWorldService>(binding, null));
        }

        [Test]
        public void Should_throw_argumentnullexception_when_constructor_is_passed_an_empty_address()
        {
            var binding = new NetTcpBinding();

            Assert.Throws<ArgumentNullException>(
                () => new ServiceHostHelper<HelloWorldService, IHelloWorldService>(binding, ""));
        }

        [Test]
        public void Should_throw_typeloadexception_when_service_is_not_a_service()
        {
            Assert.Throws<TypeLoadException>(
                () => new ServiceHostHelper<NotAService, IAmNothing>());
        }

        [Test]
        public void Should_throw_typeloadexception_when_service_is_not_a_service_but_servicecontract_is_good()
        {
            Assert.Throws<TypeLoadException>(
                () => new ServiceHostHelper<NotAService, IAmAServiceContract>());
        }

        [Test]
        public void Should_throw_typeloadexception_when_servicecontractinterface_is_not_a_service_contract_interface()
        {
            Assert.Throws<TypeLoadException>(
                () => new ServiceHostHelper<HelloWorldService, IAmNothing>());
        }

        [Test]
        public void Should_throw_typeloadexception_when_servicecontractinterface_is_wrong_service_contract_interface()
        {
            Assert.Throws<TypeLoadException>(
                () => new ServiceHostHelper<HelloWorldService, IAmAServiceContract>());
        }

        [Test]
        public void Should_have_default_endpoint()
        {
            var binding = new NetTcpBinding();
            binding.MaxReceivedMessageSize = 100000;
            binding.ReaderQuotas.MaxArrayLength = 100000;
            binding.ReaderQuotas.MaxStringContentLength = 50000;

            using (var helper = new ServiceHostHelper<HelloWorldService, IHelloWorldService>(binding))
            {
                var serviceHost = helper.CreateNewServiceHost();

                var endpoint = serviceHost.Description.Endpoints.Find(typeof(IHelloWorldService));

                endpoint.Should().NotBeNull();
                endpoint.Binding.Should().Be(binding);
                endpoint.Address.ToString().Should().Be("net.tcp://localhost:9000/");
            }
        }

        [Test]
        public void Should_use_custom_endpoint()
        {
            var binding = new WSHttpBinding();
            binding.MaxReceivedMessageSize = 100000;
            binding.ReaderQuotas.MaxArrayLength = 100000;
            binding.ReaderQuotas.MaxStringContentLength = 50000;
            binding.Security.Message.ClientCredentialType = MessageCredentialType.UserName;
            binding.Security.Message.NegotiateServiceCredential = false;

            using (var helper = new ServiceHostHelper<HelloWorldService, IHelloWorldService>(binding, "http://localhost:8081/HelloWorld"))
            {
                var serviceHost = helper.CreateNewServiceHost();

                var endpoint = serviceHost.Description.Endpoints.Find(typeof(IHelloWorldService));

                endpoint.Should().NotBeNull();
                endpoint.Binding.Should().Be(binding);
                endpoint.Address.ToString().Should().Be("http://localhost:8081/HelloWorld");
            }
        }

        [Test]
        public void Should_be_able_to_open_service_host()
        {
            using (var helper = new ServiceHostHelper<HelloWorldService, IHelloWorldService>())
            {
                var serviceHost = helper.CreateNewServiceHost();
                serviceHost.Open();

                //Thread.Sleep(1000);
                serviceHost.State.Should().Be(CommunicationState.Opened);

                serviceHost.Close();
            }
        }

        [Test]
        public void Should_be_able_to_create_and_open_ServiceHost_in_one_call()
        {
            using (var helper = new ServiceHostHelper<HelloWorldService, IHelloWorldService>())
            {
                var serviceHost = helper.OpenWcfService();
                serviceHost.Should().NotBeNull();
                serviceHost.Description.ServiceType.Should().Be(typeof(HelloWorldService));
                serviceHost.State.Should().Be(CommunicationState.Opened);
                serviceHost.Close();
            }
        }

        [Test]
        public void Should_create_channelfactory_with_defaults_endpoint()
        {
            var binding = new NetTcpBinding();
            binding.MaxReceivedMessageSize = 100000;
            binding.ReaderQuotas.MaxArrayLength = 100000;
            binding.ReaderQuotas.MaxStringContentLength = 50000;

            using (var helper = new WrappedServiceHostHelper<HelloWorldService, IHelloWorldService>())
            {
                var factory = helper.GetChannelFactory();

                factory.Should().BeOfType<ChannelFactory<IHelloWorldService>>();
                factory.Endpoint.Binding.Should().NotBeNull();
                //                factory.Endpoint.Binding.ShouldBeTheSameAs(binding); // TODO: Gives a paradoxical exception, why? "Expected: same as <System.ServiceModel.NetTcpBinding> But was:  <System.ServiceModel.NetTcpBinding>"
                factory.Endpoint.Binding.Scheme.Should().Be(binding.Scheme);
                factory.Endpoint.Binding.MessageVersion.Envelope.Should().Be(binding.EnvelopeVersion);
                factory.Endpoint.Address.ToString().Should().Be("net.tcp://localhost:9000/");

                factory.Close();
            }
        }

        [Test]
        public void Should_create_channelfactory_with_custom_endpoint()
        {
            var binding = new WSHttpBinding();
            binding.MaxReceivedMessageSize = 100000;
            binding.ReaderQuotas.MaxArrayLength = 100000;
            binding.ReaderQuotas.MaxStringContentLength = 50000;
            binding.Security.Message.ClientCredentialType = MessageCredentialType.UserName;
            binding.Security.Message.NegotiateServiceCredential = false;

            using (var helper = new ServiceHostHelper<HelloWorldService, IHelloWorldService>(binding, "http://localhost:8081/HelloWorld"))
            {
                var factory = helper.GetChannelFactory();

                factory.Should().BeOfType<ChannelFactory<IHelloWorldService>>();
                factory.Endpoint.Binding.Should().NotBeNull();
                //            factory.Endpoint.Binding.ShouldBeTheSameAs(binding); // TODO: Gives a paradoxical exception, why? "Expected: same as <System.ServiceModel.NetTcpBinding> But was:  <System.ServiceModel.NetTcpBinding>"
                factory.Endpoint.Binding.Scheme.Should().Be(binding.Scheme);
                factory.Endpoint.Binding.MessageVersion.Envelope.Should().Be(binding.EnvelopeVersion);
                factory.Endpoint.Address.ToString().Should().Be("http://localhost:8081/HelloWorld");

                factory.Close();
            }
        }

        [Test]
        public void Should_open_channelfactory_with_default_binding()
        {
            using (var helper = new WrappedServiceHostHelper<HelloWorldService, IHelloWorldService>())
            {
                helper.OpenChannelFactory();

                helper.ChannelFactory.Should().NotBeNull();
                helper.ChannelFactory.Endpoint.Should().NotBeNull();
                helper.ChannelFactory.Endpoint.Address.ToString().Should().Be("net.tcp://localhost:9000/");
            }
        }

        [Test]
        public void Should_create_client()
        {
            using (var helper = new ServiceHostHelper<HelloWorldService, IHelloWorldService>())
            {
                helper.GetClient().Should().NotBeNull();
            }
        }

        [Test]
        public void Should_get_result_from_client()
        {
            using (var helper = new ServiceHostHelper<HelloWorldService, IHelloWorldService>())
            {
                // Arrange
                helper.OpenWcfService();
                var client = helper.GetClient();

                // Act
                var hello = client.Salutate("Integration Test");

                // Assert
                hello.Salutation.Should().Be("Hello, Integration Test!");
            }
        }


        // TODO: Test for having a different binding set on client

        // TODO: Test for gracefully disposing of faulted services and clients


        // Wrapper for viewing protected members
        internal class WrappedServiceHostHelper<TSERVICE, TSERVICECONTRACT> : ServiceHostHelper<TSERVICE, TSERVICECONTRACT>
        {
            public ServiceHost ServiceHost
            {
                get { return _serviceHost; }
            }

            public ChannelFactory ChannelFactory
            {
                get { return _channelFactory; }
            }

            public bool IsChannelFactorySetup
            {
                get { return _isChannelFactorySetup; }
            }

            public Binding Binding
            {
                get { return _binding; }
            }

            public string Address
            {
                get { return _address; }
            }

            public WrappedServiceHostHelper(Binding binding, string address)
                : base(binding, address)
            {
            }

            public WrappedServiceHostHelper(Binding binding)
                : base(binding)
            {
            }

            public WrappedServiceHostHelper()
            {
            }

            public new void OpenChannelFactory()
            {
                base.OpenChannelFactory();
            }
        }

        public class NotAService { }

        public interface IAmNothing { }

        [ServiceContract]
        public interface IAmAServiceContract { }
    }
}
