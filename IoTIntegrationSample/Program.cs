using System;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using MQTTnet;
using MQTTnet.Client;
using MQTTnet.Client.Options;

namespace IoTIntegrationSample
{
    class Program
    {
        static async Task Main(string[] args)
        {
            Console.WriteLine("Starting IoT Gateway...");

            // Initialize the MQTT client
            var mqttClient = new MqttFactory().CreateMqttClient();

            // Configure MQTT options
            var mqttOptions = new MqttClientOptionsBuilder()
                .WithClientId("IoTGateway")
                .WithTcpServer("broker.hivemq.com", 1883) // Public MQTT broker for demonstration
                .WithCleanSession()
                .Build();

            // Event when a message is received from an IoT device
            mqttClient.UseApplicationMessageReceivedHandler(async e =>
            {
                var message = Encoding.UTF8.GetString(e.ApplicationMessage.Payload);
                Console.WriteLine($"Message Received from IoT Device: {message}");

                // Forward the data to the cloud server
                await ForwardToServer(message);
            });

            // Connect to the MQTT broker
            mqttClient.UseConnectedHandler(async e =>
            {
                Console.WriteLine("Connected to MQTT broker!");

                // Subscribe to an IoT topic
                await mqttClient.SubscribeAsync(new MqttTopicFilterBuilder()
                    .WithTopic("iot/devices/sensor1")
                    .Build());

                Console.WriteLine("Subscribed to topic: iot/devices/sensor1");
            });

            // Connect and start listening
            await mqttClient.ConnectAsync(mqttOptions, CancellationToken.None);

            Console.WriteLine("IoT Gateway is running. Press any key to exit...");
            Console.ReadKey();

            // Disconnect the MQTT client
            await mqttClient.DisconnectAsync();
        }

        // Function to forward IoT data to a cloud server
        private static async Task ForwardToServer(string data)
        {
            using var httpClient = new HttpClient();
            var content = new StringContent(data, Encoding.UTF8, "application/json");

            try
            {
                var response = await httpClient.PostAsync("https://example-cloud-server.com/api/iot", content);
                Console.WriteLine($"Data forwarded to server. Response: {response.StatusCode}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Failed to forward data: {ex.Message}");
            }
        }
    }
}
