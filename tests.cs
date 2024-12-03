using Moq;
using Xunit;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;
using eMap.Api.Application.Services.Customer.Abstract;
using eMap.Api.Application.Services.Import;
using eMap.Shared.Models.Customer;
using eMap.Shared.Models.Dashboard;
using eMap.Shared.Models.FileProcessing;
using eMap.Shared.Models.Import;
using eMap.Shared.Models.RequestModels;
using Microsoft.Extensions.Logging;

public class DashboardServiceTests
{
    private readonly Mock<ICustomerService> _mockCustomerService;
    private readonly Mock<IImportService> _mockImportService;
    private readonly Mock<ILogger<IDashboardService>> _mockLogger;
    private readonly DashboardService _dashboardService;

    public DashboardServiceTests()
    {
        _mockCustomerService = new Mock<ICustomerService>();
        _mockImportService = new Mock<IImportService>();
        _mockLogger = new Mock<ILogger<IDashboardService>>();
        _dashboardService = new DashboardService(_mockCustomerService.Object, _mockImportService.Object, null, _mockLogger.Object);
    }

    [Fact]
    public async Task GetDashboardAsync_ReturnsDashboardDto_WhenDataIsValid()
    {
        // Arrange
        var requestModel = new DashboardRequestModel { MonthOfSales = DateTime.Now, FileTypes = new List<FileTypeEnum>(), CustomerId = 1 };
        var token = CancellationToken.None;

        _mockImportService.Setup(x => x.GetImportHistoryAsync(It.IsAny<HistoryRequestModel>(), token))
            .ReturnsAsync(new List<ImportHistoryDto> { new ImportHistoryDto() });

        _mockImportService.Setup(x => x.GetImportStatusAsync(It.IsAny<DateTime>(), token))
            .ReturnsAsync(new List<ImportStatusDto> { new ImportStatusDto() });

        _mockCustomerService.Setup(x => x.GetCustomersNotificationsAsync(It.IsAny<DateTime>(), token))
            .ReturnsAsync(new List<CustomerNotificationDto> { new CustomerNotificationDto() });

        _mockCustomerService.Setup(x => x.GetCustomersAsync(token))
            .ReturnsAsync(new List<CustomerDto> { new CustomerDto() });

        // Act
        var result = await _dashboardService.GetDashdoardAsync(requestModel, token);

        // Assert
        Assert.NotNull(result);
    }

    [Fact]
    public async Task GetDashboardAsync_ReturnsEmptyDashboardDto_WhenImportHistoryIsEmpty()
    {
        // Arrange
        var requestModel = new DashboardRequestModel { MonthOfSales = DateTime.Now, FileTypes = new List<FileTypeEnum>(), CustomerId = 1 };
        var token = CancellationToken.None;

        _mockImportService.Setup(x => x.GetImportHistoryAsync(It.IsAny<HistoryRequestModel>(), token))
            .ReturnsAsync(new List<ImportHistoryDto>());

        _mockImportService.Setup(x => x.GetImportStatusAsync(It.IsAny<DateTime>(), token))
            .ReturnsAsync(new List<ImportStatusDto>());

        _mockCustomerService.Setup(x => x.GetCustomersNotificationsAsync(It.IsAny<DateTime>(), token))
            .ReturnsAsync(new List<CustomerNotificationDto>());

        _mockCustomerService.Setup(x => x.GetCustomersAsync(token))
            .ReturnsAsync(new List<CustomerDto>());

        // Act
        var result = await _dashboardService.GetDashdoardAsync(requestModel, token);

        // Assert
        Assert.NotNull(result);
    }

    [Fact]
    public async Task GetDashboardAsync_ThrowsArgumentNullException_WhenRequestModelIsNull()
    {
        // Arrange
        DashboardRequestModel requestModel = null;
        var token = CancellationToken.None;

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(() => _dashboardService.GetDashdoardAsync(requestModel, token));
    }

    [Fact]
    public async Task GetDashboardAsync_ThrowsOperationCanceledException_WhenTokenIsCancelled()
    {
        // Arrange
        var requestModel = new DashboardRequestModel { MonthOfSales = DateTime.Now, FileTypes = new List<FileTypeEnum>(), CustomerId = 1 };
        var tokenSource = new CancellationTokenSource();
        tokenSource.Cancel();
        var token = tokenSource.Token;

        // Act & Assert
        await Assert.ThrowsAsync<OperationCanceledException>(() => _dashboardService.GetDashdoardAsync(requestModel, token));
    }
}