using Azunt.ReasonManagement;
using Azunt.Web.Services;
using Microsoft.AspNetCore.Mvc;

namespace Azunt.Web.Components.Pages.Reasons.Apis;

[Route("api/[controller]")]
[ApiController]
public class ReasonExportController : ControllerBase
{
    private readonly IReasonRepository _repository;
    private readonly IUserService _userService;

    public ReasonExportController(IReasonRepository repository, IUserService userService)
    {
        _repository = repository;
        _userService = userService;
    }

    /// <summary>
    /// 단일 테넌트(DefaultConnection 또는 기본 In-Memory) Reasons 목록을 Excel 파일로 다운로드합니다.
    /// GET /api/ReasonExport/Excel
    /// </summary>
    [HttpGet("Excel")]
    public async Task<IActionResult> ExportToExcel()
    {
        var items = await _repository.GetAllAsync();
        var bytes = ReasonExcelExporter.ExportToExcel(items);
        var fileName = $"{DateTime.Now:yyyyMMddHHmmss}_Reasons.xlsx";

        return File(
            bytes,
            "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
            fileName);
    }

    /// <summary>
    /// 현재 사용자의 테넌트 연결 문자열을 사용하여 Reasons 목록을 Excel 파일로 다운로드합니다.
    /// GET /api/ReasonExport/ExcelByTenant
    /// </summary>
    [HttpGet("ExcelByTenant")]
    public async Task<IActionResult> ExportToExcelByTenant()
    {
        var user = _userService.GetUserNotCached();
        var connectionString = user.Tenant.ConnectionString;
        var items = await _repository.GetAllAsync(connectionString);
        var bytes = ReasonExcelExporter.ExportToExcel(items, "Tenant Reasons");
        var fileName = $"{DateTime.Now:yyyyMMddHHmmss}_Tenant_Reasons.xlsx";

        return File(
            bytes,
            "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
            fileName);
    }
}
