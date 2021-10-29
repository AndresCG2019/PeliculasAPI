using PeliculasAPI.DTOs;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace PeliculasAPI.Utilidades
{
    public class PeliculaDTO
    {
        public int Id { get; set; }
        public string Titulo { get; set; }
        public string Resumen { get; set; }
        public string Trailer { get; set; }
        public bool EnCines { get; set; }
        public DateTime FechaEstreno { get; set; }
        public string Poster { get; set; }

        public List<GeneroDTO> Generos { get; set; }
        public List<ActorDTO> Actores { get; set; }
        public List<CineDTO> Cines { get; set; }

        public int votoUsuario { get; set; }
        public double PromedioVoto { get; set; }
    }
}
