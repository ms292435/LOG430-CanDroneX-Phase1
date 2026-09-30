using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Drones.Api;
public sealed record DroneResponse(
    Guid Id,
    string Imsi,
    string Modele,
    string Statut,
    DateTime DateEnregistrement);