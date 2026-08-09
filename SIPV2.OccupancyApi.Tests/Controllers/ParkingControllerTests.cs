using Microsoft.AspNetCore.Mvc;
using SIPV2.DataModels;
using SIPV2.OccupancyApi.Contracts;
using SIPV2.OccupancyApi.Controllers;
using SIPV2.OccupancyApi.Tests.Fakes;

namespace SIPV2.OccupancyApi.Tests.Controllers;

public class ParkingControllerTests
{
    private static ParkingController CreateController(FakeParkingRepository? parkings = null, FakeOccupancyRepository? occupancy = null) =>
        new(parkings ?? new FakeParkingRepository(), occupancy ?? new FakeOccupancyRepository());

    [Fact]
    public async Task GetParkings_ReturnsAllParkings_OrderedByName()
    {
        var parkings = new FakeParkingRepository();
        parkings.Parkings.AddRange([
            new Mdparking { Id = "PKB", Name = "Beta", Type = "MY", Active = true },
            new Mdparking { Id = "PKA", Name = "Alfa", Type = "MY", Active = false },
        ]);
        var controller = CreateController(parkings);

        var result = await controller.GetParkings();

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        var dtos = Assert.IsAssignableFrom<IEnumerable<ParkingDto>>(ok.Value).ToList();
        Assert.Equal(["Alfa", "Beta"], dtos.Select(p => p.Name));
        // Ya no hay filtro de activos: deben salir todos, activos e inactivos.
        Assert.Contains(dtos, p => p.Id == "PKA");
        Assert.Contains(dtos, p => p.Id == "PKB");
    }

    [Fact]
    public async Task GetParkings_WhenNameIsNull_FallsBackToId()
    {
        var parkings = new FakeParkingRepository();
        parkings.Parkings.Add(new Mdparking { Id = "PKC", Name = null, Type = "MY", Active = true });
        var controller = CreateController(parkings);

        var result = await controller.GetParkings();

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        var dto = Assert.Single(Assert.IsAssignableFrom<IEnumerable<ParkingDto>>(ok.Value));
        Assert.Equal("PKC", dto.Name);
    }

    [Fact]
    public async Task GetParkingDetail_WithCounters_ReturnsParkingAndAllCounters()
    {
        var occupancy = new FakeOccupancyRepository();
        occupancy.Data.AddRange([
            new VoccupationActual
            {
                CounterId = "PKGR-2", ParkingId = "PKGR", ParkingName = "Parking Andalucia",
                CounterCode = "2", CounterName = "Todos", ControlSystem = "MY", Capacity = 618, CurrentLevel = 316, Perc = 51.13m,
            },
            new VoccupationActual
            {
                CounterId = "PKGR-1", ParkingId = "PKGR", ParkingName = "Parking Andalucia",
                CounterCode = "1", CounterName = "Abonados", ControlSystem = "MY", Capacity = 1000, CurrentLevel = 75, Perc = 7.5m,
            },
        ]);
        var controller = CreateController(occupancy: occupancy);

        var result = await controller.GetParkingDetail("PKGR");

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        var detail = Assert.IsType<ParkingDetailDto>(ok.Value);
        Assert.Equal("PKGR", detail.ParkingId);
        Assert.Equal("Parking Andalucia", detail.ParkingName);
        Assert.Equal(2, detail.Counters.Count);
        Assert.Equal(["Abonados", "Todos"], detail.Counters.Select(c => c.CounterName)); // orden por CounterName
    }

    [Fact]
    public async Task GetParkingDetail_WithoutCounters_ButParkingExists_ReturnsEmptyCountersList()
    {
        var parkings = new FakeParkingRepository();
        parkings.Parkings.Add(new Mdparking { Id = "PKZ", Name = "Sin contadores", Type = "MY", Active = true });
        var controller = CreateController(parkings);

        var result = await controller.GetParkingDetail("PKZ");

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        var detail = Assert.IsType<ParkingDetailDto>(ok.Value);
        Assert.Equal("Sin contadores", detail.ParkingName);
        Assert.Empty(detail.Counters);
    }

    [Fact]
    public async Task GetParkingDetail_UnknownId_ReturnsNotFound()
    {
        var controller = CreateController();

        var result = await controller.GetParkingDetail("NO-EXISTE");

        Assert.IsType<NotFoundResult>(result.Result);
    }
}
