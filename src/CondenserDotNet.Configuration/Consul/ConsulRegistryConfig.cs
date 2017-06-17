namespace CondenserDotNet.Configuration.Consul
{
    public class ConsulRegistryConfig
    {
        public IKeyParser KeyParser { get; set; } = new SimpleKeyValueParser();
    }
}
