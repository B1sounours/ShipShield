using Mono.Cecil;
using Mono.Cecil.Cil;
using Sandbox.ModAPI;
using ShipShieldPlugin.Utility;
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
    class ShipShieldE
    {
        //   public BeaconEntity CenterBeaconEntity { get; set; }
        public IMyFaction Faction { get; set; }

        //public Vector3D CenterBeaconPosition { get; set; }

        public IMyEntity CenterBeaconIMyEntity { get; set; }
    }



    public class ShipShield : SEModAPIExtensions.API.Plugin.IPlugin
    {
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

                InjectionHelper.Initialize();
                InjectionHelper.WaitForIntializationCompletion();

                var MyProjectile = SEModAPIInternal.API.Common.SandboxGameAssemblyWrapper.Instance.GetAssemblyType("Sandbox.Game.Weapons", "MyProjectile");

     

                var sandboxasm = AssemblyDefinition.ReadAssembly("Sandbox.Game.dll");
                if (sandboxasm != null)
                {
                    var MyProjectileDef = sandboxasm.Modules
                        .SelectMany(m => m.Types)
                        .Where(t => t.Name == "MyProjectile").First();

                    var DoDamageDef = MyProjectileDef.Methods.FirstOrDefault(f => f.Name == "DoDamage");
                    var work = DoDamageDef.Body.GetILProcessor();

                    if (DoDamageDef.Body.Instructions.First().OpCode != OpCodes.Call)
                        
                    {
                        return;
                    }
                    work.InsertBefore(DoDamageDef.Body.Instructions.First(), work.Create(OpCodes.Call,
                     sandboxasm.MainModule.Import(typeof(ShipShield).GetMethod("MyProjectileDoDamage"))));
                    work.InsertAfter(DoDamageDef.Body.Instructions.First(), work.Create(OpCodes.Brfalse_S, DoDamageDef.Body.Instructions[3]));


                    work.InsertBefore(DoDamageDef.Body.Instructions.First(), work.Create(OpCodes.Ldarga_S, DoDamageDef.Parameters[1]));
                    work.InsertBefore(DoDamageDef.Body.Instructions.First(), work.Create(OpCodes.Ldarga_S, DoDamageDef.Parameters[0]));


                    MemoryStream sandboxStream = new MemoryStream();

                    sandboxasm.Write(sandboxStream);


                    var changeSandboxAssembly = Assembly.Load(sandboxStream.ToArray());

                    var changeMyProjectile = changeSandboxAssembly.GetType("Sandbox.Game.Weapons.MyProjectile");

                    MethodInfo changeMyProjectileDoDamage = changeMyProjectile.GetMethod("DoDamage", BindingFlags.NonPublic | BindingFlags.Instance);
                    var ilCodes = changeMyProjectileDoDamage.GetMethodBody().GetILAsByteArray();

                    FileStream fs = new FileStream("outil.bin", FileMode.Create);
                    fs.Write(ilCodes, 0, ilCodes.Length);
                    fs.Close();


                    MethodInfo replaceMethod = this.GetType().GetMethod("MyProjectileDoDamage", BindingFlags.Public | BindingFlags.Static);
                    Logging.WriteLineAndConsole("replaceMethod.MetadataToken"+ replaceMethod.MetadataToken);


                    MethodInfo MyProjectileDoDamage = changeMyProjectile.GetMethod("DoDamage", BindingFlags.NonPublic | BindingFlags.Instance);
                    InjectionHelper.UpdateILCodes(MyProjectileDoDamage, ilCodes);


                    var mpi = changeSandboxAssembly.CreateInstance("Sandbox.Game.Weapons.MyProjectile");



                    MyProjectileDoDamage.Invoke(mpi, new object[] { new VRageMath.Vector3D(), null });
                }
            }
            catch (Exception e)
            {
                Logging.WriteLineAndConsole(e.ToString());
            }

        }


        public static bool MyProjectileDoDamage(ref VRageMath.Vector3D hitPosition, ref IMyEntity damagedEntity)
        {
            var cubeGrid = damagedEntity as IMyCubeGrid;
            if (cubeGrid==null)
            {
                return true;
            }
            try
            {
                List<IMySlimBlock> shieldblocks = new List<IMySlimBlock>();
                cubeGrid.GetBlocks(shieldblocks, (block) => {
                    if (block.FatBlock==null)
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
            catch(Exception e)
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
