using Mono.Cecil;
using Mono.Cecil.Cil;
using Sandbox.ModAPI;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace ShipShieldPlugin
{
  

    public class ShipShield
    {
        public static string CubeBlockNamespace = "6DDCED906C852CFDABA0B56B84D0BD74";
        public static string CubeBlockClass = "54A8BE425EAC4A11BFF922CFB5FF89D0";

        //public static string CubeBlockDamageBlockMethod = "165EAAEA972A8C5D69F391D030C48869";
        //public static string CubeBlockDamageBlockMethod = "DoDamage";
        public static string CubeBlockDamageBlockMethod = "165EAAEA972A8C5D69F391D030C48869";


        public static readonly string ShipShieldSubtypeId = "ShipShield";
        public ShipShield()
        {

        }



        public Guid Id
        {
            get
            {
                GuidAttribute guidAttr = (GuidAttribute)typeof(ShipShield).Assembly.GetCustomAttributes(typeof(GuidAttribute), true)[0];
                return new Guid(guidAttr.Value);
            }
        }



        public void Init()
        {
            try
            {



                File.Copy("Sandbox.Game.dll", "Sandbox.Game.dll.tmp", true);
                var sandboxasm = AssemblyDefinition.ReadAssembly("Sandbox.Game.dll.tmp");
                if (sandboxasm != null)
                {
                    bool iswrite = false;

                    iswrite |= MyProjectileChange(sandboxasm);


                    iswrite |= MyMissileChange(sandboxasm);



                    if (iswrite)
                    {
                        MemoryStream sandboxStream = new MemoryStream();
                        File.Copy("Sandbox.Game.dll", "Sandbox.Game.dll.bak", true);
                        sandboxasm.Write("Sandbox.Game.dll");
                    }

                    //var changeSandboxAssembly = Assembly.Load(sandboxStream.ToArray());

                    //var changeMyProjectile = changeSandboxAssembly.GetType("Sandbox.Game.Weapons.MyProjectile");

                    //MethodInfo changeMyProjectileDoDamage = changeMyProjectile.GetMethod("DoDamage", BindingFlags.NonPublic | BindingFlags.Instance);
                    //var ilCodes = changeMyProjectileDoDamage.GetMethodBody().GetILAsByteArray();


                    //MethodInfo MyProjectileDoDamage = MyProjectile.GetMethod("DoDamage", BindingFlags.NonPublic | BindingFlags.Instance);
                    //InjectionHelper.UpdateILCodes(MyProjectileDoDamage, ilCodes);
                }
            }
            catch (Exception e)
            {
                //Logging.WriteLineAndConsole(e.ToString());
            }

        }

        private static bool MyMissileChange(AssemblyDefinition sandboxasm)
        {
            //var MyMissileDef = sandboxasm.Modules
            //.SelectMany(m => m.Types)
            //.Where(t => t.Name == "MyProjectile").First();

            //var UpdateBeforeSimulationDef = MyMissileDef.Methods.FirstOrDefault(f => f.Name == "DoDamage");
            //var work = UpdateBeforeSimulationDef.Body.GetILProcessor();

            //if (UpdateBeforeSimulationDef.Body.Instructions.First().OpCode == OpCodes.Call)
            //{
            //    work.InsertBefore(UpdateBeforeSimulationDef.Body.Instructions.First(), work.Create(OpCodes.Call,
            //        sandboxasm.MainModule.Import(typeof(ShipShield).GetMethod("MyProjectileDoDamage"))));
            //    work.InsertAfter(UpdateBeforeSimulationDef.Body.Instructions.First(), work.Create(OpCodes.Brfalse_S, UpdateBeforeSimulationDef.Body.Instructions[3]));
            //    work.InsertBefore(UpdateBeforeSimulationDef.Body.Instructions.First(), work.Create(OpCodes.Ldarga_S, UpdateBeforeSimulationDef.Parameters[1]));
            //    work.InsertBefore(UpdateBeforeSimulationDef.Body.Instructions.First(), work.Create(OpCodes.Ldarga_S, UpdateBeforeSimulationDef.Parameters[0]));
            //    return true;
            //}
            return false;
            
        }

        private static bool MyProjectileChange(AssemblyDefinition sandboxasm)
        {
            var MyProjectileDef = sandboxasm.Modules
                .SelectMany(m => m.Types)
                .Where(t => t.Name == "MyProjectile").First();

            var DoDamageDef = MyProjectileDef.Methods.FirstOrDefault(f => f.Name == "DoDamage");
            var work = DoDamageDef.Body.GetILProcessor();

            if (DoDamageDef.Body.Instructions.First().OpCode == OpCodes.Call)
            {
                work.InsertBefore(DoDamageDef.Body.Instructions.First(), work.Create(OpCodes.Call,
                    sandboxasm.MainModule.Import(typeof(ShipShield).GetMethod("MyProjectileDoDamage"))));
                work.InsertAfter(DoDamageDef.Body.Instructions.First(), work.Create(OpCodes.Brfalse_S, DoDamageDef.Body.Instructions[3]));
                work.InsertBefore(DoDamageDef.Body.Instructions.First(), work.Create(OpCodes.Ldarga_S, DoDamageDef.Parameters[1]));
                work.InsertBefore(DoDamageDef.Body.Instructions.First(), work.Create(OpCodes.Ldarga_S, DoDamageDef.Parameters[0]));
                return true;
            }
            return false;
        }


        public static bool MyProjectileDoDamage(ref VRageMath.Vector3D hitPosition, ref IMyEntity damagedEntity)
        {
            var cubeGrid = damagedEntity as IMyCubeGrid;
            if (cubeGrid == null)
            {
                return true;
            }
            try
            {
                List<IMySlimBlock> shieldblocks = new List<IMySlimBlock>();
                cubeGrid.GetBlocks(shieldblocks, (block) =>
                {
                    if (block.FatBlock == null)
                    {
                        return false;
                    }
                    return block.FatBlock.BlockDefinition.SubtypeId == ShipShieldSubtypeId;
                });

                foreach (var shielditem in shieldblocks)
                {
                    if (shielditem.FatBlock.IsFunctional && shielditem.FatBlock.IsWorking)
                    {
                        hitPosition = shielditem.FatBlock.WorldMatrix.Translation;
                        break;
                    }
                }
            }
            catch (Exception e)
            {
                // Logging.WriteLineAndConsole(e.ToString());
            }



            //Logging.WriteLineAndConsole("DoDamage");
            return true;
        }

        public void Shutdown()
        {
            // Logging.WriteLineAndConsole("Shutdown");
        }

        public void Update()
        {

        }

        public string Name
        {
            get { return "Dedicated Server ShipShield"; }
        }

        public string Version
        {
            get { return typeof(ShipShield).Assembly.GetName().Version.ToString(); }
        }



    }
}
