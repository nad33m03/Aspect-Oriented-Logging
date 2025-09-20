# Application Flow Documentation

## API Endpoint: GET /WeatherForecast/Get

### Flow Sequence

- → **Service**: `WeatherForecastService.GetForecasts`
  - Called by: `WeatherForecastController.Get` (Line: 24)
  - Parameters: ```json
{
  "days": 5
}```
- → **Repository**: `WeatherForecastRepository.GetForecasts`
  - Called by: `WeatherForecastService.GetForecasts` (Line: 17)
  - Parameters: ```json
{
  "days": 5
}```
- ← **Repository**: `WeatherForecastRepository.GetForecasts`
  - Duration: 1ms
  - Returns: `IEnumerable`1`
- ← **Service**: `WeatherForecastService.GetForecasts`
  - Duration: 56ms
  - Returns: `IEnumerable`1`

**Total Flow Duration:** 57ms

---

