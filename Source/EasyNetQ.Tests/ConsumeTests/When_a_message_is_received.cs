﻿// ReSharper disable InconsistentNaming
using System;
using System.Text;
using System.Threading;
using EasyNetQ.Events;
using EasyNetQ.Tests.Mocking;
using FluentAssertions;
using NSubstitute;
using RabbitMQ.Client.Framing;
using Xunit;

namespace EasyNetQ.Tests.ConsumeTests
{
    public class When_a_message_is_received : IDisposable
    {
        private MockBuilder mockBuilder;
        private MyMessage deliveredMyMessage;
        private MyOtherMessage deliveredMyOtherMessage;

        public When_a_message_is_received()
        {
            //mockBuilder = new MockBuilder(x => x.Register<IEasyNetQLogger, ConsoleLogger>());
            mockBuilder = new MockBuilder();

            mockBuilder.Bus.Receive("the_queue", x => x
                .Add<MyMessage>(message => deliveredMyMessage = message)
                .Add<MyOtherMessage>(message => deliveredMyOtherMessage = message));

            DeliverMessage("{ Text: \"Hello World :)\" }", "EasyNetQ.Tests.MyMessage, EasyNetQ.Tests");
            DeliverMessage("{ Text: \"Goodbye Cruel World!\" }", "EasyNetQ.Tests.MyOtherMessage, EasyNetQ.Tests");
            DeliverMessage("{ Text: \"Shouldn't get this\" }", "EasyNetQ.Tests.Unknown, EasyNetQ.Tests");
        }

        public void Dispose()
        {
            mockBuilder.Bus.Dispose();
        }

        [Fact]
        public void Should_deliver_MyMessage()
        {
            deliveredMyMessage.Should().NotBeNull();
            deliveredMyMessage.Text.Should().Be("Hello World :)");
        }

        [Fact]
        public void Should_deliver_MyOtherMessage()
        {
            deliveredMyOtherMessage.Should().NotBeNull();
            deliveredMyOtherMessage.Text.Should().Be("Goodbye Cruel World!");
        }

        private void DeliverMessage(string message, string type)
        {
            var properties = new BasicProperties
            {
                Type = type,
                CorrelationId = "the_correlation_id"
            };
            var body = Encoding.UTF8.GetBytes(message);

            mockBuilder.Consumers[0].HandleBasicDeliver(
                "consumer tag",
                0,
                false,
                "the_exchange",
                "the_routing_key",
                properties,
                body
                );

            WaitForMessageDispatchToComplete();
        }

        private void WaitForMessageDispatchToComplete()
        {
            // wait for the subscription thread to handle the message ...
            var autoResetEvent = new AutoResetEvent(false);
            mockBuilder.EventBus.Subscribe<AckEvent>(x => autoResetEvent.Set());
            autoResetEvent.WaitOne(1000);
        }        
    }
}

// ReSharper restore InconsistentNaming