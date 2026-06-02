using System.IO.Compression;
using System.Security;
using System.Text;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Localization;
using MyMiniLMS.Data;
using MyMiniLMS.Models;

namespace MyMiniLMS.Services;

public class ReportService
{
    private readonly ApplicationDbContext _context;
    private readonly IStringLocalizer<SharedResource> _localizer;

    public ReportService(
        ApplicationDbContext context,
        IStringLocalizer<SharedResource> localizer)
    {
        _context = context;
        _localizer = localizer;
    }

    public async Task<byte[]?> BuildStudentCourseXlsxAsync(string studentId, int courseId)
    {
        var rows = await GetStudentCourseRowsAsync(studentId, courseId);
        return rows == null ? null : BuildXlsx(rows);
    }

    public async Task<byte[]?> BuildStudentCourseDocxAsync(string studentId, int courseId)
    {
        var rows = await GetStudentCourseRowsAsync(studentId, courseId);
        return rows == null ? null : BuildDocx(_localizer["ReportStudentTitle"].Value, rows);
    }

    public async Task<byte[]> BuildOverdueXlsxAsync(int? courseId, string? teacherId, bool isAdmin)
    {
        var rows = await GetOverdueRowsAsync(courseId, teacherId, isAdmin);
        return BuildXlsx(rows);
    }

    public async Task<byte[]> BuildOverdueDocxAsync(int? courseId, string? teacherId, bool isAdmin)
    {
        var rows = await GetOverdueRowsAsync(courseId, teacherId, isAdmin);
        return BuildDocx(_localizer["ReportOverdueTitle"].Value, rows);
    }

    private async Task<List<string[]>?> GetStudentCourseRowsAsync(string studentId, int courseId)
    {
        var isAssigned = await _context.StudentCourses
            .AnyAsync(sc => sc.StudentId == studentId && sc.CourseId == courseId);

        if (!isAssigned)
        {
            return null;
        }

        var assignments = await _context.StudentAssignments
            .Where(sa => sa.StudentId == studentId && sa.Assignment.CourseId == courseId)
            .Include(sa => sa.Assignment)
                .ThenInclude(a => a.Course)
            .OrderBy(sa => sa.Assignment.Deadline)
            .Select(sa => new
            {
                CourseName = sa.Assignment.Course!.Name,
                sa.Assignment.Title,
                Description = sa.Assignment.Description ?? string.Empty,
                sa.Assignment.Deadline,
                sa.Status,
                sa.StartedAt,
                sa.CompletedAt
            })
            .ToListAsync();

        var rows = assignments
            .Select(sa => new[]
            {
                sa.CourseName,
                sa.Title,
                sa.Description,
                sa.Deadline.HasValue ? sa.Deadline.Value.ToString("yyyy-MM-dd HH:mm") : string.Empty,
                GetStatusText(sa.Status),
                sa.StartedAt.HasValue ? sa.StartedAt.Value.ToString("yyyy-MM-dd HH:mm") : string.Empty,
                sa.CompletedAt.HasValue ? sa.CompletedAt.Value.ToString("yyyy-MM-dd HH:mm") : string.Empty
            })
            .ToList();

        rows.Insert(0, new[]
        {
            _localizer["ReportCourse"].Value,
            _localizer["ReportAssignment"].Value,
            _localizer["ReportDescription"].Value,
            _localizer["ReportDeadlineUtc"].Value,
            _localizer["ReportStatus"].Value,
            _localizer["ReportStartedUtc"].Value,
            _localizer["ReportCompletedUtc"].Value
        });

        return rows;
    }

    private async Task<List<string[]>> GetOverdueRowsAsync(int? courseId, string? teacherId, bool isAdmin)
    {
        var now = DateTime.UtcNow;

        var query = _context.StudentAssignments
            .Where(sa =>
                sa.Assignment.Deadline.HasValue &&
                sa.Assignment.Deadline.Value < now &&
                sa.Status != AssignmentStatus.Completed);

        if (courseId.HasValue)
        {
            query = query.Where(sa => sa.Assignment.CourseId == courseId.Value);
        }

        if (!isAdmin)
        {
            query = query.Where(sa => sa.Assignment.Course!.TeacherId == teacherId);
        }

        var overdueAssignments = await query
            .Include(sa => sa.Student)
            .Include(sa => sa.Assignment)
                .ThenInclude(a => a.Course)
            .OrderBy(sa => sa.Assignment.Course!.Name)
            .ThenBy(sa => sa.Assignment.Deadline)
            .Select(sa => new
            {
                CourseName = sa.Assignment.Course!.Name,
                sa.Student.FullName,
                Email = sa.Student.Email ?? string.Empty,
                sa.Assignment.Title,
                sa.Assignment.Deadline,
                sa.Status
            })
            .ToListAsync();

        var rows = overdueAssignments
            .Select(sa => new[]
            {
                sa.CourseName,
                sa.FullName,
                sa.Email,
                sa.Title,
                sa.Deadline.HasValue ? sa.Deadline.Value.ToString("yyyy-MM-dd HH:mm") : string.Empty,
                GetStatusText(sa.Status)
            })
            .ToList();

        rows.Insert(0, new[]
        {
            _localizer["ReportCourse"].Value,
            _localizer["ReportStudent"].Value,
            _localizer["ReportEmail"].Value,
            _localizer["ReportAssignment"].Value,
            _localizer["ReportDeadlineUtc"].Value,
            _localizer["ReportStatus"].Value
        });

        return rows;
    }

    private static byte[] BuildXlsx(List<string[]> rows)
    {
        using var stream = new MemoryStream();
        using (var archive = new ZipArchive(stream, ZipArchiveMode.Create, true))
        {
            AddEntry(archive, "[Content_Types].xml", """
                <?xml version="1.0" encoding="UTF-8"?>
                <Types xmlns="http://schemas.openxmlformats.org/package/2006/content-types">
                  <Default Extension="rels" ContentType="application/vnd.openxmlformats-package.relationships+xml"/>
                  <Default Extension="xml" ContentType="application/xml"/>
                  <Override PartName="/xl/workbook.xml" ContentType="application/vnd.openxmlformats-officedocument.spreadsheetml.sheet.main+xml"/>
                  <Override PartName="/xl/worksheets/sheet1.xml" ContentType="application/vnd.openxmlformats-officedocument.spreadsheetml.worksheet+xml"/>
                </Types>
                """);
            AddEntry(archive, "_rels/.rels", """
                <?xml version="1.0" encoding="UTF-8"?>
                <Relationships xmlns="http://schemas.openxmlformats.org/package/2006/relationships">
                  <Relationship Id="rId1" Type="http://schemas.openxmlformats.org/officeDocument/2006/relationships/officeDocument" Target="xl/workbook.xml"/>
                </Relationships>
                """);
            AddEntry(archive, "xl/workbook.xml", """
                <?xml version="1.0" encoding="UTF-8"?>
                <workbook xmlns="http://schemas.openxmlformats.org/spreadsheetml/2006/main" xmlns:r="http://schemas.openxmlformats.org/officeDocument/2006/relationships">
                  <sheets><sheet name="Report" sheetId="1" r:id="rId1"/></sheets>
                </workbook>
                """);
            AddEntry(archive, "xl/_rels/workbook.xml.rels", """
                <?xml version="1.0" encoding="UTF-8"?>
                <Relationships xmlns="http://schemas.openxmlformats.org/package/2006/relationships">
                  <Relationship Id="rId1" Type="http://schemas.openxmlformats.org/officeDocument/2006/relationships/worksheet" Target="worksheets/sheet1.xml"/>
                </Relationships>
                """);
            AddEntry(archive, "xl/worksheets/sheet1.xml", BuildSheetXml(rows));
        }

        return stream.ToArray();
    }

    private static byte[] BuildDocx(string title, List<string[]> rows)
    {
        using var stream = new MemoryStream();
        using (var archive = new ZipArchive(stream, ZipArchiveMode.Create, true))
        {
            AddEntry(archive, "[Content_Types].xml", """
                <?xml version="1.0" encoding="UTF-8"?>
                <Types xmlns="http://schemas.openxmlformats.org/package/2006/content-types">
                  <Default Extension="rels" ContentType="application/vnd.openxmlformats-package.relationships+xml"/>
                  <Default Extension="xml" ContentType="application/xml"/>
                  <Override PartName="/word/document.xml" ContentType="application/vnd.openxmlformats-officedocument.wordprocessingml.document.main+xml"/>
                </Types>
                """);
            AddEntry(archive, "_rels/.rels", """
                <?xml version="1.0" encoding="UTF-8"?>
                <Relationships xmlns="http://schemas.openxmlformats.org/package/2006/relationships">
                  <Relationship Id="rId1" Type="http://schemas.openxmlformats.org/officeDocument/2006/relationships/officeDocument" Target="word/document.xml"/>
                </Relationships>
                """);
            AddEntry(archive, "word/document.xml", BuildDocumentXml(title, rows));
        }

        return stream.ToArray();
    }

    private static string BuildSheetXml(List<string[]> rows)
    {
        var xml = new StringBuilder();
        xml.Append("<?xml version=\"1.0\" encoding=\"UTF-8\"?><worksheet xmlns=\"http://schemas.openxmlformats.org/spreadsheetml/2006/main\"><sheetData>");

        for (var rowIndex = 0; rowIndex < rows.Count; rowIndex++)
        {
            xml.Append("<row r=\"").Append(rowIndex + 1).Append("\">");

            for (var colIndex = 0; colIndex < rows[rowIndex].Length; colIndex++)
            {
                xml.Append("<c r=\"")
                    .Append(GetCellName(colIndex, rowIndex + 1))
                    .Append("\" t=\"inlineStr\"><is><t>")
                    .Append(SecurityElement.Escape(rows[rowIndex][colIndex]))
                    .Append("</t></is></c>");
            }

            xml.Append("</row>");
        }

        xml.Append("</sheetData></worksheet>");
        return xml.ToString();
    }

    private static string BuildDocumentXml(string title, List<string[]> rows)
    {
        var xml = new StringBuilder();
        xml.Append("""<?xml version="1.0" encoding="UTF-8"?><w:document xmlns:w="http://schemas.openxmlformats.org/wordprocessingml/2006/main"><w:body>""");
        xml.Append("<w:p><w:r><w:t>").Append(SecurityElement.Escape(title)).Append("</w:t></w:r></w:p>");

        foreach (var row in rows)
        {
            xml.Append("<w:p><w:r><w:t>")
                .Append(SecurityElement.Escape(string.Join(" | ", row)))
                .Append("</w:t></w:r></w:p>");
        }

        xml.Append("<w:sectPr/></w:body></w:document>");
        return xml.ToString();
    }

    private static string GetCellName(int colIndex, int rowIndex)
    {
        var dividend = colIndex + 1;
        var columnName = string.Empty;

        while (dividend > 0)
        {
            var modulo = (dividend - 1) % 26;
            columnName = Convert.ToChar('A' + modulo) + columnName;
            dividend = (dividend - modulo) / 26;
        }

        return $"{columnName}{rowIndex}";
    }

    private string GetStatusText(AssignmentStatus status)
    {
        return status switch
        {
            AssignmentStatus.NotStarted => _localizer["Status_NotStarted"].Value,
            AssignmentStatus.InProgress => _localizer["Status_InProgress"].Value,
            AssignmentStatus.Completed => _localizer["Status_Completed"].Value,
            _ => status.ToString()
        };
    }

    private static void AddEntry(ZipArchive archive, string name, string content)
    {
        var entry = archive.CreateEntry(name);

        using var writer = new StreamWriter(entry.Open(), Encoding.UTF8);
        writer.Write(content);
    }
}
