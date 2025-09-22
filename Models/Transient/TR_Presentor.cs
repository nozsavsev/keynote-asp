using keynote_asp.Helpers;
using Keynote_asp.Nauth.API_GEN.Models;

namespace keynote_asp.Models.Transient
{
    public class TR_Presentor : TR_BaseEntity
    {
        public UserDTO? nauthUser { get; set; } = null;
        public string Name
        {
            get
            {
                if (nauthUser?.Name != null)
                {
                    return nauthUser.Name;
                }
                return field ?? "Anonymous";
            }
            set { field = value; }
        }
    }

    public class TR_PresentorDTO : TR_BaseEntityDTO
    {
        public string Name { get; set; } = string.Empty;
    }
}
