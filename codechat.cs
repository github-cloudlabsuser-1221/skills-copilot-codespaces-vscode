using eMap.Api.Application.Services.Customer.Abstract;
using eMap.Api.Application.Services.Import;
using eMap.Shared.Extensions;
using eMap.Shared.Helpers;
using eMap.Shared.Models.Customer;
using eMap.Shared.Models.Dashboard;
using eMap.Shared.Models.FileProcessing;
using eMap.Shared.Models.Import;
using eMap.Shared.Models.RequestModels;
using EnsureThat;
using Microsoft.Extensions.Logging;
using ProcessingTypeEnum = eMap.Shared.Models.ProcessingTypeEnum;

namespace eMap.Api.Application.Services.Dashboard
{
    public class DashboardService : BaseService, IDashboardService
    {
        private readonly ICustomerService _customerService;
        private readonly IImportService _importService;

        public DashboardService(
            ICustomerService customerService,
            IImportService importService,
            IServiceProvider serviceProvider,
            ILogger<IDashboardService> logger):base(serviceProvider, logger)
        {
            _customerService = Ensure.Any.IsNotNull(customerService, nameof(customerService));
            _importService = Ensure.Any.IsNotNull(importService, nameof(importService));
        }

        public async Task<DashboardDto> GetDashdoardAsync(DashboardRequestModel requestModel, CancellationToken token)
        {
            var historyData = (await _importService.GetImportHistoryAsync(new HistoryRequestModel {MonthOfSales = requestModel.MonthOfSales, FileTypes = requestModel.FileTypes, CustomerId = requestModel.CustomerId }, token)).ToList();
            var loadStatus = await _importService.GetImportStatusAsync(requestModel.MonthOfSales, token);
            var notifications = await _customerService.GetCustomersNotificationsAsync(requestModel.MonthOfSales.AddMonths(1), token);
            var customers = await _customerService.GetCustomersAsync(token);

            historyData.ForEach(d => d.DateTimeStamp = DateTimeHelper.UtcToPst(d.DateTimeStamp)); //default to PST
            var dashboardData = GenerateDashboardData(historyData, loadStatus, customers, notifications, requestModel.FileTypes);
            return dashboardData;
        }

        private DashboardDto GenerateDashboardData(List<ImportHistoryDto> historyData, IEnumerable<ImportStatusDto> loadStatus, IEnumerable<CustomerDto> customers, IEnumerable<CustomerNotificationDto> notifications, IEnumerable<FileTypeEnum> fileTypes)
        {
            var result = new DashboardDto();

            foreach (var customer in customers)
            {
                foreach (var processingType in customer.CustomerProcessingTypes)
                {
                    var isEdiProcessing = processingType.ProcessingType == ProcessingTypeEnum.EDI;
                    var customerHistoryData = historyData
                        .Where(x => x.CustomerId == customer.Id && 
                        (!isEdiProcessing || (isEdiProcessing && x.ProcessingType == ProcessingTypeEnum.EDI.ToDescription())));
                    var relatedNotificationsNumber = notifications.Count(x => x.CustomerId == customer.Id);

                    ////for EDI only EDI processing type should
                    //if (processingType.ProcessingType == ProcessingTypeEnum.EDI)
                    //{
                    //    customerHistoryData = customerHistoryData.Where(d => d.ProcessingType == ProcessingTypeEnum.EDI.ToDescription());
                    //}

                    var dataTypes = new List<FileTypeEnum>();
                    if (customer.IsOverlayCustomer && fileTypes.Contains(FileTypeEnum.Overlay))
                    {
                        dataTypes.Add(FileTypeEnum.Overlay);
                        dataTypes.Add(FileTypeEnum.EndUserAndOverlay);
                    }
                    if (customer.IsEndUserCustomer && fileTypes.Contains(FileTypeEnum.EndUser))
                    {
                        dataTypes.Add(FileTypeEnum.EndUser);
                        dataTypes.Add(FileTypeEnum.EndUserAndOverlay);
                    }

                    var customerLoadStatus = loadStatus.Where(x => x.CustomerId == customer.Id && dataTypes.Contains(x.FileType) && (processingType.ProcessingType != ProcessingTypeEnum.EDI || (processingType.ProcessingType == ProcessingTypeEnum.EDI && x.ProcessingType == processingType.ProcessingType)));
                    var status = GetStatusModel(customer, customerHistoryData, relatedNotificationsNumber, customerLoadStatus, processingType.ProcessingType);

                    switch (processingType.ProcessingType)
                    {
                        case ProcessingTypeEnum.Auto:
                            {
                                result.AutoCustomersStatus.Add(status);
                                break;
                            }
                        case ProcessingTypeEnum.Manual:
                            {
                                result.ManualCustomersStatus.Add(status);
                                break;
                            }
                        case ProcessingTypeEnum.EDI:
                            {
                                result.EDICustomersStatus.Add(status);
                                break;
                            }
                        default:
                            throw new ArgumentOutOfRangeException("Unsupported processing type");
                    }
                }
            }

            return result;
        }

        private CustomerStatus GetStatusModel(CustomerDto customer, IEnumerable<ImportHistoryDto> customerHistoryData, int relatedNotificationsNumber, IEnumerable<ImportStatusDto> customerLoadStatus, ProcessingTypeEnum processingType)
        {
            CustomerStatus dashboardItem = new CustomerStatus {
                CustomerId = customer.Id,
                CustomerName = customer.DisplayName,
                History = customerHistoryData
            };

            FillStatusItem(dashboardItem, customerHistoryData, relatedNotificationsNumber, customer, customerLoadStatus);

            return dashboardItem;
        }

        private void FillStatusItem(CustomerStatus dashboardRecord, IEnumerable<ImportHistoryDto> customerHistoryData, int relatedNotificationsNumber, CustomerDto customer, IEnumerable<ImportStatusDto> customerLoadStatus)
        {
            var latestRecord = customerHistoryData
                .OrderByDescending(x => x.DateTimeStamp)
                .FirstOrDefault();

            if (customerLoadStatus != null && customerLoadStatus.Any(m => m.IsLoaded))
            {
                var loadedModels = customerLoadStatus.Where(m => m.IsLoaded);
                var successfullyLoadedRecord = customerHistoryData.FindSuccessfullyProcessedRecordIfExists();
                dashboardRecord.LoadStatus = FileLoadStatus.Success;
                dashboardRecord.FileType = string.Join(",", loadedModels.Select(m => m.FileType.ToDescription()).Distinct());
                dashboardRecord.LoadMode = latestRecord?.LoadMode;
                dashboardRecord.DataLoadStatus = dashboardRecord.LoadStatus.ToDescription();
                dashboardRecord.RowsCount = loadedModels.Sum(m => m.RowsCount);
                dashboardRecord.FileName = string.Join(",", loadedModels.Select(m => m.FileName));
                dashboardRecord.ReceivedFileDate = loadedModels.First().ImportDate;
                dashboardRecord.ImportedBy = successfullyLoadedRecord?.UserNum;
            }
            else  // missing file, no data, error
            {
                var lastRecordIsError = customerHistoryData
                .OrderByDescending(x => x.DateTimeStamp)
                .FirstOrDefault(x => x.Status == ImportStatusEnum.Error.ToString());

                dashboardRecord.FileName = string.Empty;
                dashboardRecord.LoadMode = string.Empty;
                dashboardRecord.ReceivedFileDate = null;
                dashboardRecord.RowsCount = 0;
                dashboardRecord.DetectProperWarningRecordStatus(relatedNotificationsNumber, customer.IsActiveInApp, lastRecordIsError != null);
            }
        }
    }
}
