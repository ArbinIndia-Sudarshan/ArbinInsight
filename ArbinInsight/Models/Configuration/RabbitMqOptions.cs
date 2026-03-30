namespace ArbinInsight.Models.Configuration
{
    public class RabbitMqOptions
    {
        public string HostName { get; set; } = "localhost";
        public int Port { get; set; } = 5672;
        public string UserName { get; set; } = "guest";
        public string Password { get; set; } = "guest";
        public string VirtualHost { get; set; } = "/";
        public string Exchange { get; set; } = "arbin.remote.sync";
        public string Queue { get; set; } = "arbin.remote.fetch";
        public string RoutingKey { get; set; } = "remote.testdata";
        public bool EnableDashboardConsumer { get; set; } = true;
    }
}
