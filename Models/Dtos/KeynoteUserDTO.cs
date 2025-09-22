
using System.Collections.Generic;

namespace keynote_asp.Dtos
{
    public class KeynoteUserDTO
    {
        public string Id { get; set; } = null!;
        public List<KeynoteDTO> Keynotes { get; set; } = new();
    }
}
