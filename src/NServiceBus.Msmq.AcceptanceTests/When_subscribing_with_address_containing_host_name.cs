﻿namespace NServiceBus.Transport.Msmq.AcceptanceTests
{
    using System.Threading.Tasks;
    using AcceptanceTesting;
    using AcceptanceTesting.Customization;
    using Config;
    using Features;
    using NServiceBus.AcceptanceTests;
    using NServiceBus.AcceptanceTests.EndpointTemplates;
    using NUnit.Framework;
    using Support;

    public class When_subscribing_with_address_containing_host_name : NServiceBusAcceptanceTest
    {
        [Test]
        public async Task Event_should_be_delivered()
        {
            var context = await Scenario.Define<Context>()
                .WithEndpoint<Publisher>(b => b.When(c => c.Subscribed, (session, c) => session.Publish(new MyEvent())))
                .WithEndpoint<Subscriber>()
                .Done(c => c.Delivered)
                .Run();

            Assert.IsTrue(context.Delivered);
        }

        public class Context : ScenarioContext
        {
            public bool Subscribed { get; set; }
            public bool Delivered { get; set; }
        }

        public class Publisher : EndpointConfigurationBuilder
        {
            public Publisher()
            {
                EndpointSetup<DefaultPublisher>(b =>
                {
                    b.DisableFeature<AutoSubscribe>();
                    b.OnEndpointSubscribed<Context>((s, context) => { context.Subscribed = true; });
                });
            }
        }

        static string PublisherEndpoint => Conventions.EndpointNamingConvention(typeof(Publisher));

        public class Subscriber : EndpointConfigurationBuilder
        {
            public Subscriber()
            {
                EndpointSetup<DefaultServer>()
                    .WithConfig<UnicastBusConfig>(c =>
                    {
                        c.MessageEndpointMappings = new MessageEndpointMappingCollection();
                        c.MessageEndpointMappings.Add(new MessageEndpointMapping
                        {
                            Endpoint = $"{PublisherEndpoint}@{RuntimeEnvironment.MachineName}",
                            AssemblyName = typeof(Publisher).Assembly.GetName().Name,
                            TypeFullName = typeof(MyEvent).FullName
                        });
                    });
            }

            public class MyEventHandler : IHandleMessages<MyEvent>
            {
                public Context Context { get; set; }

                public Task Handle(MyEvent messageThatIsEnlisted, IMessageHandlerContext context)
                {
                    Context.Delivered = true;
                    return Task.FromResult(0);
                }
            }
        }

        public class MyEvent : IEvent
        {
        }
    }
}