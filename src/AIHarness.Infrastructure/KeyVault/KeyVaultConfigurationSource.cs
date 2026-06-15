using Azure.Identity;
using Azure.Security.KeyVault.Secrets;
using Microsoft.Extensions.Configuration;

namespace AIHarness.Infrastructure.KeyVault;

public sealed class KeyVaultConfigurationSource : IConfigurationSource
{
    private readonly Uri _vaultUri;

    public KeyVaultConfigurationSource(Uri vaultUri) => _vaultUri = vaultUri;

    public IConfigurationProvider Build(IConfigurationBuilder builder)
        => new KeyVaultConfigurationProvider(_vaultUri);
}

public sealed class KeyVaultConfigurationProvider : ConfigurationProvider
{
    private readonly SecretClient _secretClient;

    public KeyVaultConfigurationProvider(Uri vaultUri)
    {
        _secretClient = new SecretClient(vaultUri, new DefaultAzureCredential());
    }

    public override void Load() => LoadAsync().GetAwaiter().GetResult();

    private async Task LoadAsync()
    {
        var data = new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase);
        await foreach (var props in _secretClient.GetPropertiesOfSecretsAsync())
        {
            if (props.Enabled != true) continue;
            var secret = await _secretClient.GetSecretAsync(props.Name);
            // Key Vault uses double-hyphen as section separator; map to colon for .NET config
            var configKey = props.Name.Replace("--", ":");
            data[configKey] = secret.Value.Value;
        }
        Data = data;
    }
}
