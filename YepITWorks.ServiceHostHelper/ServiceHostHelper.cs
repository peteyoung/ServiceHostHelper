using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Security;
using System.ServiceModel;
using System.ServiceModel.Channels;
using System.ServiceModel.Description;

namespace YepITWorks.ServiceHostHelper
{
	public class ServiceHostHelper<TSERVICE, TSERVICECONTRACT> : IDisposable
	{
		protected Binding _binding;
		protected String _address;

		protected ServiceHost _serviceHost;
		protected ChannelFactory<TSERVICECONTRACT> _channelFactory;
		protected bool _isChannelFactorySetup;

		protected List<IEndpointBehavior> _endPointBehaviors;
		protected List<IServiceBehavior> _serviceBehaviors;

        public ServiceHostHelper() : this(GetDefaultBinding()) {}
        public ServiceHostHelper(Binding binding) : this(binding, "net.tcp://localhost:9000/") {}
        public ServiceHostHelper(Binding binding, String address) : this(binding, address, null, null) {}
        public ServiceHostHelper(Binding binding, String address, List<IEndpointBehavior> endpointBehaviors) : this(binding, address, endpointBehaviors, null) {}
        public ServiceHostHelper(Binding binding, String address, List<IServiceBehavior> serviceBehaviors) : this(binding, address, null, serviceBehaviors) {}

		public ServiceHostHelper(
			Binding binding, 
			String address, 
			List<IEndpointBehavior> endpointBehaviors, 
			List<IServiceBehavior> serviceBehaviors)
		{
			_binding = binding;
			_address = address;
			_endPointBehaviors = endpointBehaviors;
			_serviceBehaviors = serviceBehaviors;

			if (_binding == null)
				throw new ArgumentNullException("An explicit binding cannot be null.");

			if (String.IsNullOrEmpty(_address))
				throw new ArgumentNullException("An explicit address cannot be null or empty.");

			ValidateServiceContract();
		}

		public ServiceHost CreateNewServiceHost()
		{
			var host = new ServiceHost(typeof(TSERVICE));
			if (_endPointBehaviors != null)
			{
				var endpoint =
					host.AddServiceEndpoint(typeof(TSERVICECONTRACT), _binding, _address);
				_endPointBehaviors.ForEach(eb => endpoint.Behaviors.Add(eb));
			}
			else
			{
				host.AddServiceEndpoint(typeof (TSERVICECONTRACT), _binding, _address);
			}

			if (_serviceBehaviors != null)
				_serviceBehaviors.ForEach(
					sb =>
						{
							ValidateServiceBehavior(host, sb);
							host.Description.Behaviors.Add(sb);
						});

			var debugBehavior = (from sb in host.Description.Behaviors
								where sb.GetType() == typeof (ServiceDebugBehavior)
								select sb).FirstOrDefault();

			((ServiceDebugBehavior) debugBehavior).IncludeExceptionDetailInFaults = true;

			return host;
		}

		private void ValidateServiceBehavior(ServiceHost host, IServiceBehavior serviceBehavior)
		{
			var foundBehavior = (from sb in host.Description.Behaviors
								 where sb.GetType() == serviceBehavior.GetType()
								 select sb).FirstOrDefault();

			if (foundBehavior != null)
				throw new Exception(
					string.Format(
						"ServiceBehavior {0} already exists on host",
						serviceBehavior.GetType().Name));
		}

		protected void ValidateServiceContract()
		{
			Type[] interfaces = typeof(TSERVICE).GetInterfaces();
			Type serviceContractInterface = null;

			foreach (var iface in interfaces)
			{
				var attributes = iface.GetCustomAttributes(typeof(ServiceContractAttribute), false);

				if (attributes.Length > 0)
					serviceContractInterface = iface;
			}

			if (serviceContractInterface == null)
				throw new TypeLoadException("No ServiceContract found on any interfaces for type " + typeof(TSERVICE).Name);

			if (serviceContractInterface != null && serviceContractInterface != typeof(TSERVICECONTRACT))
				throw new TypeLoadException(
					"ServiceContract \"" + serviceContractInterface.Name +
					"\" found on service \"" + typeof(TSERVICE).Name +
					"\" does not match type passed into constructor \"" + typeof(TSERVICECONTRACT).Name);
		}

		protected static Binding GetDefaultBinding()
		{
			var binding = new NetTcpBinding();
			binding.MaxReceivedMessageSize = 5242880; // 5M for incoming messages
			binding.ReaderQuotas.MaxArrayLength = 26214400; // 25M for array length in outgoing message
			binding.ReaderQuotas.MaxBytesPerRead = 26214400; // 25M for size of outgoing message
			binding.ReaderQuotas.MaxStringContentLength = 26214400; // 25M for string length in outgoing message
			binding.Security.Transport.ProtectionLevel = ProtectionLevel.EncryptAndSign;
			return binding;
		}

		public ChannelFactory<TSERVICECONTRACT> GetChannelFactory()
		{
			return GetChannelFactory(_binding);
		}

		public ChannelFactory<TSERVICECONTRACT> GetChannelFactory(Binding binding)
		{
			var contractDescription = ContractDescription.GetContract(typeof(TSERVICECONTRACT));
			var endpoint = new ServiceEndpoint(contractDescription, binding, new EndpointAddress(_address));
			if(_endPointBehaviors != null)
				_endPointBehaviors.ForEach( eb => endpoint.Behaviors.Add(eb));
			var channelFactory = new ChannelFactory<TSERVICECONTRACT>(endpoint);

			return channelFactory;
		}

		public ServiceHost OpenWcfService()
		{
			_serviceHost = CreateNewServiceHost();
			_serviceHost.Open();
			return _serviceHost;
		}

		public virtual void TearDownWcfService()
		{
			if (_serviceHost != null)
			{
				switch (_serviceHost.State)
				{
					case (CommunicationState.Opened):
					case (CommunicationState.Opening):
						_serviceHost.Close();
						break;

					case (CommunicationState.Faulted):
						_serviceHost.Abort();
						_serviceHost.Close();
						break;
				}
			}
		}

		protected virtual void OpenChannelFactory()
		{
			OpenChannelFactory(_binding);
		}

		protected virtual void OpenChannelFactory(Binding binding)
		{
			if (!_isChannelFactorySetup)
			{
				_channelFactory = GetChannelFactory(binding);

				if (_channelFactory.State != CommunicationState.Opening && _channelFactory.State != CommunicationState.Opened)
					_channelFactory.Open();

				_isChannelFactorySetup = true;
			}
		}

		public virtual void TearDownChannelFactory()
		{
			if (_channelFactory != null)
			{
				switch (_channelFactory.State)
				{
					case (CommunicationState.Opened):
					case (CommunicationState.Opening):
						try
						{
							_channelFactory.Close();
						}
						catch (Exception)
						{
							_channelFactory.Abort();
						}
						break;

					case (CommunicationState.Faulted):
						_channelFactory.Abort();
						_channelFactory.Close();
						break;
				}
			}
		}

		public virtual TSERVICECONTRACT GetClient()
		{
			return GetClient(_binding);
		}

		public virtual TSERVICECONTRACT GetClient(Binding binding)
		{
			OpenChannelFactory(binding);
			TSERVICECONTRACT client = _channelFactory.CreateChannel();
			return client;
		}

		// yay! let's use "using"
		public void Dispose()
		{
			TearDownChannelFactory();
			TearDownWcfService();
		}
	}
}

// see http://blogs.conchango.com/howardvanrooijen/archive/2007/03/14/Configuring-WCF-Services-for-Unit-Testing.aspx
