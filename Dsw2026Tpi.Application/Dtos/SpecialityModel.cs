

namespace Dsw2026Tpi.Application.Dtos
{
    public record SpecialityModel
    {
        public record Request(string name, string Description);
        public record Response(Guid id,string name, string Description);
    }

}
