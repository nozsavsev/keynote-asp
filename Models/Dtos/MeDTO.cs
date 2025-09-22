
using Keynote_asp.Nauth.API_GEN.Models;
using System.Collections.Generic;

namespace keynote_asp.Dtos
{
    public class MeDTO
    {
        public KeynoteUserDTO KeynoteUser { get; set; } = null!;
        public UserDTO NauthUser { get; set; } = null!;
    }
}
