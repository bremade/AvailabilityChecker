#include <Arduino.h>
#include <PubSubClient.h>
#include <ESP8266WiFi.h>

#define LED_PIN 13
#define MSG_BUFFER_SIZE	(50)

const char* ssid = "WLAN NAME";
const char* password = "WLAN PASSWORD";
const char* mqtt_server = "BROKER IP";
const char* mqtt_topic = "TOPIC";
const char* mqtt_user = "BROKER USERNAME";
const char* mqtt_password = "BROKER PASSWORD";

WiFiClient espClient;
PubSubClient client (espClient);
unsigned long lastMsg = 0;
char msg[MSG_BUFFER_SIZE];
int value = 0;

void setupWifi () {
  WiFi.begin(ssid, password);
  Serial.print("Connecting to ");
  Serial.print(ssid);

  while (WiFi.status() != WL_CONNECTED) {
    delay(500);
    Serial.print(".");
  }

  Serial.println('\n');
  Serial.println("Connection established!");  
  Serial.print("IP address:\t");
  Serial.println(WiFi.localIP());
}

void callback(char* topic, byte* payload, unsigned int length) {
  Serial.print("Message arrived [");
  Serial.print(topic);
  Serial.print("] ");
  for (int i = 0; i < length; i++) {
    Serial.print((char)payload[i]);
  }
  Serial.println();

  // Switch on the LED if an 1 was received as first character
  if ((char)payload[0] == '1') {
    analogWrite(LED_PIN, 100);
  } else {
    analogWrite(LED_PIN, 0);
  }

}

void reconnect() {
  while (!client.connected()) {
    Serial.print("Attempting MQTT connection...");

    // Create a random client ID
    String clientId = "ESP8266Client-";
    clientId += String(random(0xffff), HEX);

    if (client.connect(clientId.c_str(), mqtt_user, mqtt_password)) {
      Serial.println("connected");
      client.subscribe(mqtt_topic);
    } else {
      Serial.print("failed, rc=");
      Serial.print(client.state());
      Serial.println(" try again in 5 seconds");
      delay(5000);
    }
  }
}

void setup() {
  Serial.begin(115200);
  delay(10);
  Serial.println('\n');

  pinMode(LED_PIN, OUTPUT);
  // For powerbank usage
  analogWrite(LED_PIN, 100);

  setupWifi();

  client.setServer(mqtt_server, 1883);
  client.setCallback(callback);
}

void loop() {
  if (!client.connected()) {
    reconnect();
  }
  client.loop();
}