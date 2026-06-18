using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Spreadsheet;

namespace Azunt.ReasonManagement;

/// <summary>
/// EPPlus 없이 Microsoft Open XML SDK로 Reasons 목록을 Excel 파일로 생성하는 도우미 클래스입니다.
/// ASP.NET Core Controller에서는 이 클래스가 반환하는 byte[]를 File(...)로 내려주면 됩니다.
/// </summary>
public static class ReasonExcelExporter
{
    public static byte[] ExportToExcel(IEnumerable<Reason> reasons, string worksheetName = "Reasons")
    {
        ArgumentNullException.ThrowIfNull(reasons);

        using var stream = new MemoryStream();

        using (var document = SpreadsheetDocument.Create(stream, SpreadsheetDocumentType.Workbook))
        {
            var workbookPart = document.AddWorkbookPart();
            workbookPart.Workbook = new Workbook();

            var worksheetPart = workbookPart.AddNewPart<WorksheetPart>();
            var sheetData = new SheetData();
            worksheetPart.Worksheet = new Worksheet(sheetData);

            var sheets = workbookPart.Workbook.AppendChild(new Sheets());
            var sheet = new Sheet
            {
                Id = workbookPart.GetIdOfPart(worksheetPart),
                SheetId = 1,
                Name = string.IsNullOrWhiteSpace(worksheetName) ? "Reasons" : worksheetName
            };
            sheets.Append(sheet);

            sheetData.Append(CreateRow("Id", "Name", "Content", "CreatedAt", "Active", "CreatedBy"));

            foreach (var reason in reasons)
            {
                sheetData.Append(CreateRow(
                    reason.Id.ToString(),
                    reason.Name ?? string.Empty,
                    reason.Content ?? string.Empty,
                    reason.CreatedAt == default ? string.Empty : reason.CreatedAt.ToLocalTime().ToString("yyyy-MM-dd HH:mm:ss zzz"),
                    reason.Active?.ToString() ?? string.Empty,
                    reason.CreatedBy ?? string.Empty));
            }

            workbookPart.Workbook.Save();
        }

        return stream.ToArray();
    }

    private static Row CreateRow(params string[] values)
    {
        var row = new Row();

        foreach (var value in values)
        {
            row.Append(CreateTextCell(value));
        }

        return row;
    }

    private static Cell CreateTextCell(string value)
    {
        return new Cell
        {
            DataType = CellValues.InlineString,
            InlineString = new InlineString(new DocumentFormat.OpenXml.Spreadsheet.Text(value ?? string.Empty))
        };
    }
}
