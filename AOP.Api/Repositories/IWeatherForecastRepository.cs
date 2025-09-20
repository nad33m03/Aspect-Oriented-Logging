using System.Collections.Generic;

namespace AOP.Api.Repositories
{
    public interface IWeatherForecastRepository
    {
        IEnumerable<WeatherForecast> GetForecasts(int days);
    }
}
