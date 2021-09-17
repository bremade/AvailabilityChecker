using System;
using System.Text;
using System.Threading;
using uPLibrary.Networking.M2Mqtt;
using uPLibrary.Networking.M2Mqtt.Messages;

namespace AvailabilityChecker
{
    class MicStatusChecker
    {
        public readonly DateTime END_DATE = DateTime.Now.AddHours(8);

        private readonly MqttClient mqttClient;
        private readonly String mqttTopic;

        public MicStatusChecker(MqttClient mqttClient, String mqttTopic, String mqttUser, String mqttPassword)
        {
            this.mqttClient = mqttClient;
            this.mqttTopic = mqttTopic;

            string clientId = Guid.NewGuid().ToString();
            mqttClient.Connect(clientId, mqttUser, mqttPassword);
        }

        public void CheckMicStatus(Object stateInfo)
        {
            // Exit condition
            AutoResetEvent autoEvent = (AutoResetEvent)stateInfo;
            if (DateTime.Compare(DateTime.Now, END_DATE) >= 0)
            {
                autoEvent.Set();
            }

            // Check status and send to esp
            if (Engine.findProcessInSystray())
            {
                Console.WriteLine("{0:h:mm:ss.fff} Current Status: ✗ - NOT Available\n",
                          DateTime.Now);

                mqttClient.Publish(mqttTopic, Encoding.UTF8.GetBytes("1"), MqttMsgBase.QOS_LEVEL_EXACTLY_ONCE, false);
            }
            else
            {
                Console.WriteLine("{0:h:mm:ss.fff} Current Status: ✓ - Available\n",
                          DateTime.Now);

                mqttClient.Publish(mqttTopic, Encoding.UTF8.GetBytes("0"), MqttMsgBase.QOS_LEVEL_EXACTLY_ONCE, false);
            };
        }
    }
}
