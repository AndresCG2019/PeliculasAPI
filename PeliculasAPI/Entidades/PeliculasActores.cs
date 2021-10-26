using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace PeliculasAPI.Entidades
{
    public class PeliculasActores
    {
        public int PeliculaId { get; set; }
        public int ActorId { get; set; }
        public Pelicula Pelicula { get; set; }
        public Actor Actor { get; set; }
        public string Personaje { get; set; }
        public int Orden { get; set; }
    }
}
