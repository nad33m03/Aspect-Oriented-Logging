using System.Collections.Generic;
using AOP.Api.Repositories;

namespace AOP.Api.Services
{
    public class WeatherForecastService : IWeatherForecastService
    {
        private readonly IWeatherForecastRepository _repository;

        public WeatherForecastService(IWeatherForecastRepository repository)
        {
            _repository = repository;
        }

        public IEnumerable<WeatherForecast> GetForecasts(int days)
        {
            return _repository.GetForecasts(days);
        }
    }
}
