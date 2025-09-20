using System.Collections.Generic;

namespace AOP.Api.Services
{
    public interface IWeatherForecastService
    {
        IEnumerable<WeatherForecast> GetForecasts(int days);
    }
}
