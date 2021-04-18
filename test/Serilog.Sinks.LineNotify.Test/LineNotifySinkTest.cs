using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Xunit;

namespace Serilog.Sinks.LineNotify.Test
{
    public class LineNotifySinkTest
    {
        [Fact]
        public async Task TestName()
        {
            //Given
            IConfigurationBuilder configurationBuilder = new ConfigurationBuilder()
                .AddJsonFile("appsettings.json", optional : false, reloadOnChange : true);
            IConfiguration configuration = configurationBuilder.Build();

            Log.Logger = new LoggerConfiguration()
                .ReadFrom.Configuration(configuration)
                .CreateLogger();

            //When
            for (int i = 0; i < 10; i++)
            {
                Log.Logger.Information("Serilog.Sinks.LineNotify.Test..");
            }

            //Then
            await Task.Delay(3000);
        }
    }
}