using Azunt.ReasonManagement;
using Microsoft.AspNetCore.Mvc;

namespace Azunt.Web.Components.Pages.Reasons.Apis;

[Route("api/[controller]")]
[ApiController]
public class ReasonExportController : ControllerBase
{
    private readonly IReasonRepository _repository;

    public ReasonExportController(IReasonRepository repository)
    {
        _repository = repository;
    }

    /// <summary>
    /// Reasons 목록을 Microsoft Open XML SDK 기반 Excel 파일로 다운로드합니다.
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
}
