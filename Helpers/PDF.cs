using UglyToad.PdfPig;

namespace keynote_asp.Helpers
{
    public class PDF
    {
        public static int CountPages(Stream stream)
        {
            using var pdf = PdfDocument.Open(stream);
            return pdf.NumberOfPages;
        }
    }
}
