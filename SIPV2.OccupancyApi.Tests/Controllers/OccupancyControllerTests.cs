using Microsoft.AspNetCore.Mvc;
using SIPV2.DataModels;
using SIPV2.OccupancyApi.Contracts;
using SIPV2.OccupancyApi.Controllers;
using SIPV2.OccupancyApi.Tests.Fakes;

namespace SIPV2.OccupancyApi.Tests.Controllers;

public class OccupancyControllerTests
{
    private static VoccupationActual Counter(string parkingId, string counterId, string counterName) => new()
    {
        ParkingId = parkingId,
        ParkingName = $"Parking {parkingId}",
        CounterId = counterId,
        CounterCode = counterId,
        CounterName = counterName,
        ControlSystem = "MY",
        CurrentLevel = 10,
    };

    [Fact]
    public async Task GetCurrentOccupancy_NoFilters_ReturnsEverything()
    {
        var occupancy = new FakeOccupancyRepository();
        occupancy.Data.AddRange([Counter("PKGR", "PKGR-1", "Libres"), Counter("PKTE", "PKTE-1", "Ocupadas")]);
        var controller = new OccupancyController(occupancy);

        var result = await controller.GetCurrentOccupancy();

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        Assert.Equal(2, Assert.IsAssignableFrom<IEnumerable<CurrentOccupancyDto>>(ok.Value).Count());
    }

    [Fact]
    public async Task GetCurrentOccupancy_FiltersByParkingId()
    {
        var occupancy = new FakeOccupancyRepository();
        occupancy.Data.AddRange([Counter("PKGR", "PKGR-1", "Libres"), Counter("PKTE", "PKTE-1", "Ocupadas")]);
        var controller = new OccupancyController(occupancy);

        var result = await controller.GetCurrentOccupancy(parkingId: "PKGR");

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        var dto = Assert.Single(Assert.IsAssignableFrom<IEnumerable<CurrentOccupancyDto>>(ok.Value));
        Assert.Equal("PKGR", dto.ParkingId);
    }

    [Fact]
    public async Task GetCurrentOccupancy_FiltersByCounterName_PartialCaseInsensitive()
    {
        var occupancy = new FakeOccupancyRepository();
        occupancy.Data.AddRange([Counter("PKGR", "PKGR-1", "Total plazas Libres"), Counter("PKTE", "PKTE-1", "Total plazas Ocupadas")]);
        var controller = new OccupancyController(occupancy);

        var result = await controller.GetCurrentOccupancy(counterName: "libres");

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        var dto = Assert.Single(Assert.IsAssignableFrom<IEnumerable<CurrentOccupancyDto>>(ok.Value));
        Assert.Equal("PKGR-1", dto.CounterId);
    }

    [Fact]
    public async Task GetCurrentOccupancyByCounterIds_ReturnsMatchingCounters()
    {
        var occupancy = new FakeOccupancyRepository();
        occupancy.Data.AddRange([Counter("PKGR", "PKGR-1", "Libres"), Counter("PKGR", "PKGR-2", "Ocupadas"), Counter("PKTE", "PKTE-1", "Libres")]);
        var controller = new OccupancyController(occupancy);

        var result = await controller.GetCurrentOccupancyByCounterIds(new CurrentOccupancyByCounterIdsRequest(["PKGR-1", "PKTE-1"]));

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        var dtos = Assert.IsAssignableFrom<IEnumerable<CurrentOccupancyDto>>(ok.Value).ToList();
        Assert.Equal(["PKGR-1", "PKTE-1"], dtos.Select(d => d.CounterId).OrderBy(id => id));
    }

    [Fact]
    public async Task GetCurrentOccupancyByCounterIds_EmptyList_ReturnsBadRequest()
    {
        var controller = new OccupancyController(new FakeOccupancyRepository());

        var result = await controller.GetCurrentOccupancyByCounterIds(new CurrentOccupancyByCounterIdsRequest([]));

        Assert.IsType<BadRequestObjectResult>(result.Result);
    }
}
