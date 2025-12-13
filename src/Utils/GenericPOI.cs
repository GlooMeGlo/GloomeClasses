using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Vintagestory.API.MathTools;
using Vintagestory.GameContent;

namespace GloomeClasses.src.Utils {
    public class GenericPOI : IPointOfInterest {

        public Vec3d Position => pos;
        public string Type => type;

        private readonly string type;
        private readonly Vec3d pos;

        public GenericPOI(Vec3d vec, string type) {
            pos = vec;
            this.type = type;
        }

        public GenericPOI(double x, double y, double z, string type) {
            pos = new Vec3d(x, y, z);
            this.type = type;
        }

        public static GenericPOI GetEmptyPOI(string type) {
            return new GenericPOI(0, 0, 0, type);
        }
    }
}
