using Eagle_web_api.Controllers;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;

namespace EagleTest.ControllersTest
{
    public class WeatherForecastTest
    {
        [Fact]
        public void Get_ShouldReturnFiveWeatherForecasts()
        {
            // Arrange
            var loggerMock = new Mock<ILogger<WeatherForecastController>>();
            var controller = new WeatherForecastController(loggerMock.Object);

            // Act
            var result = controller.Get().ToList();

            // Assert
            result.Should().HaveCount(5);

            foreach (var forecast in result)
            {
                forecast.TemperatureC.Should().BeInRange(-20, 55);
                forecast.Summary.Should().NotBeNullOrEmpty();
                forecast.Date.Should().BeAfter(DateOnly.FromDateTime(DateTime.Now));
            }

            var allowedSummaries = new[]
            {
                "Freezing", "Bracing", "Chilly", "Cool", "Mild",
                "Warm", "Balmy", "Hot", "Sweltering", "Scorching"
            };

            result.All(f => allowedSummaries.Contains(f.Summary)).Should().BeTrue();
        }
    }
}
