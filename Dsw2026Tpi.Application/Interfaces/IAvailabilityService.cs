using System;
using System.Collections.Generic;
using System.Text;
using Dsw2026Tpi.Application.Dtos;

namespace Dsw2026Tpi.Application.Interfaces;

public interface IAvailabilityService
{
    Task<IEnumerable<AvailabilityModel.SaveResponse>> Create(AvailabilityModel.Request request);
    Task<IEnumerable<AvailabilityModel.SaveResponse>> Update(AvailabilityModel.Request request);
    Task<IEnumerable<AvailabilityModel.Response>> GetByDoctor(Guid doctorId);
}
