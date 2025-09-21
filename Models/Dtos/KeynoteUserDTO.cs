
using System.Collections.Generic;

namespace keynote_asp.Dtos
{
    public class KeynoteUserDTO
    {
        public string Id { get; set; }
        public ICollection<KeynoteDTO> Keynotes { get; set; }
    }
}
