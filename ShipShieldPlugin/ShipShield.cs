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

        public static readonly string ShipShieldSubtypeId = "ShipShield";
        public static readonly float DamageRate = 0.1f;
        public static readonly float MissileDamageRate = 0.01f;

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

                //File.Copy("Sandbox.Game.dll", "Sandbox.Game.dll.tmp", true);
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
                    Console.WriteLine("patch ok");
                   
                }
            }
            catch (Exception e)
            {
                Console.WriteLine("patch error");
                Console.WriteLine(e.ToString());
            }
            Console.ReadLine();
        }

        private static bool MyMissileChange(AssemblyDefinition sandboxasm)
        {
            var MyMissileDef = sandboxasm.Modules
            .SelectMany(m => m.Types)
            .Where(t => t.Name == "MyMissile").First();

            var UpdateBeforeSimulationDef = MyMissileDef.Methods.FirstOrDefault(f => f.Name == "UpdateBeforeSimulation");
            var work = UpdateBeforeSimulationDef.Body.GetILProcessor();

            var instructions = UpdateBeforeSimulationDef.Body.Instructions;

            if (instructions[1].OpCode == OpCodes.Ldarg_0)
            {
                if (instructions[25].OpCode == OpCodes.Ldloca_S)
                {
                    int index = 24;

    //ldloca.s 3
    //IL_0078: ldsfld bool '=Qf8bCAQhfztrGrjRhh0cHn7vi2='::'=wpvApXa3alUwNavF69ICBCHA6B='
    //IL_007d: brtrue.s IL_0086

    //IL_007f: ldc.r4 200
    //IL_0084: br.s IL_0091

    //IL_0086: ldarg.0
    //IL_0087: ldfld class Sandbox.Definitions.MyMissileAmmoDefinition Sandbox.Game.Weapons.MyMissile::m_missileAmmoDefinition
    //IL_008c: ldfld float32 Sandbox.Definitions.MyMissileAmmoDefinition::MissileExplosionDamage
                    var ld1 = (FieldDefinition)instructions[31].Operand;
                    var ld2 = (FieldDefinition)instructions[36].Operand;
                    var ld3 = (FieldDefinition)instructions[37].Operand;

                    //work.InsertAfter(instructions[index++], work.Create(OpCodes.Ldloca_S, work.Body.Variables[3]));
                    //work.InsertAfter(instructions[index++], work.Create(OpCodes.Ldsfld, ld1));
                    //var IL_0078 = instructions[index  ];
                    //work.InsertAfter(instructions[index++], work.Create(OpCodes.Ldc_R4,200f));
                    //var IL_007f = instructions[index  ];
                 

                    work.InsertAfter(instructions[index++], work.Create(OpCodes.Ldarg_0));
                    //var IL_0086 = instructions[index ];
                    work.InsertAfter(instructions[index++], work.Create(OpCodes.Ldfld, ld2));
                    work.InsertAfter(instructions[index++], work.Create(OpCodes.Ldfld, ld3));
           
                    //work.InsertAfter(IL_0078, work.Create(OpCodes.Brtrue_S, IL_0086));
                    //index++;
                  

                    work.InsertAfter(instructions[index++], work.Create(OpCodes.Ldarg_0));
                    //work.InsertAfter(IL_007f, work.Create(OpCodes.Br_S, instructions[index]));
                    //index++;


                    work.InsertAfter(instructions[index++], work.Create(OpCodes.Ldloc_0));
                    work.InsertAfter(instructions[index++], work.Create(OpCodes.Ldloca_S, work.Body.Variables[1]));

                    work.InsertAfter(instructions[index++], work.Create(OpCodes.Call,
                        sandboxasm.MainModule.Import(typeof(ShipShield).GetMethod("MyMissileUpdateBeforeSimulation"))));

                    Instruction o = (Instruction)instructions[5].Operand;
                    work.InsertAfter(instructions[index++], work.Create(OpCodes.Brfalse, o));




                    Console.WriteLine("MyMissileChange ok");
                    return true;
                }
            }
            return false;
            
        }

        private static bool MyProjectileChange(AssemblyDefinition sandboxasm)
        {
            var MyProjectileDef = sandboxasm.Modules
                .SelectMany(m => m.Types)
                .Where(t => t.Name == "MyProjectile").First();

            var DoDamageDef = MyProjectileDef.Methods.FirstOrDefault(f => f.Name == "DoDamage");
            var work = DoDamageDef.Body.GetILProcessor();

            var instructions = DoDamageDef.Body.Instructions;
            if (instructions[1].OpCode == OpCodes.Call)
            {
                int index = 0;
                var ld1 = (FieldDefinition)instructions[44].Operand;
                var ld2 = (FieldDefinition)instructions[44].Operand;

                work.InsertAfter(instructions[index++], work.Create(OpCodes.Ldarg_0));
                work.InsertAfter(instructions[index++], work.Create(OpCodes.Ldfld, ld1));
                work.InsertAfter(instructions[index++], work.Create(OpCodes.Ldfld, ld2));

                work.InsertAfter(instructions[index++], work.Create(OpCodes.Ldarga_S, DoDamageDef.Parameters[0]));
                work.InsertAfter(instructions[index++], work.Create(OpCodes.Ldarga_S, DoDamageDef.Parameters[1]));   
                work.InsertAfter(instructions[index++], work.Create(OpCodes.Call,
                    sandboxasm.MainModule.Import(typeof(ShipShield).GetMethod("MyProjectileDoDamage"))));
                work.InsertAfter(instructions[index++], work.Create(OpCodes.Brfalse_S, DoDamageDef.Body.Instructions[index+2]));

                Console.WriteLine("MyProjectileChange ok");
                return true;
            }
            return false;
        }

        public static bool MyMissileUpdateBeforeSimulation(float damage, IMyEntity missile, float missileExplosionRadius, ref VRageMath.BoundingSphereD ed)
        {
            try
            {
                damage *= MissileDamageRate;
                var entitys = MyAPIGateway.Entities.GetEntitiesInSphere(ref ed);
                bool isdef = false;
                foreach (var entityitem in entitys)
                {
                    var cubeGrid = entityitem as IMyCubeGrid;
                    if (cubeGrid == null)
                    {
                        continue;
                    }

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
                            SEModAPIInternal.API.Entity.Sector.SectorObject.CubeGridEntity cube =
                                new SEModAPIInternal.API.Entity.Sector.SectorObject.CubeGridEntity((Sandbox.Common.ObjectBuilders.MyObjectBuilder_CubeGrid)cubeGrid.GetObjectBuilder(), cubeGrid);

                            SEModAPIInternal.API.Entity.Sector.SectorObject.CubeGrid.CubeBlock.BatteryBlockEntity BatteryBlockEntity =
                                new SEModAPIInternal.API.Entity.Sector.SectorObject.CubeGrid.CubeBlock.BatteryBlockEntity(cube, (Sandbox.Common.ObjectBuilders.MyObjectBuilder_BatteryBlock)shielditem.FatBlock.GetObjectBuilderCubeBlock(), shielditem);

                            if (BatteryBlockEntity.CurrentStoredPower > damage)
                            {
                                BatteryBlockEntity.CurrentStoredPower -= damage;
                                isdef = true;
                                break;
                            }
                            else
                            {
                                if (BatteryBlockEntity.CurrentStoredPower > 0)
                                {
                                    BatteryBlockEntity.CurrentStoredPower = 0;
                                    isdef = true;
                                    break;
                                }
                            }

                        }
                    }
                }
                if (isdef == true)
                {
                    return false;
                }
            }
            catch(Exception e)
            {

            }

            return true;
        }
        public static bool MyProjectileDoDamage(float damage, ref VRageMath.Vector3D hitPosition, ref IMyEntity damagedEntity)
        {
            var cubeGrid = damagedEntity as IMyCubeGrid;
            if (cubeGrid == null)
            {
                return true;
            }
            try
            {
                damage *= DamageRate;
                List<IMySlimBlock> shieldblocks = new List<IMySlimBlock>();
                cubeGrid.GetBlocks(shieldblocks, (block) =>
                {
                    if (block.FatBlock == null)
                    {
                        return false;
                    }
                    return block.FatBlock.BlockDefinition.SubtypeId == ShipShieldSubtypeId;
                });
                bool isdef = false;
                foreach (var shielditem in shieldblocks)
                {
                    if (shielditem.FatBlock.IsFunctional && shielditem.FatBlock.IsWorking)
                    {
                        // hitPosition = shielditem.FatBlock.WorldMatrix.Translation;
                        SEModAPIInternal.API.Entity.Sector.SectorObject.CubeGridEntity cube =
                              new SEModAPIInternal.API.Entity.Sector.SectorObject.CubeGridEntity((Sandbox.Common.ObjectBuilders.MyObjectBuilder_CubeGrid)cubeGrid.GetObjectBuilder(), cubeGrid);
                        var builder = (Sandbox.Common.ObjectBuilders.MyObjectBuilder_BatteryBlock)shielditem.FatBlock.GetObjectBuilderCubeBlock();
                        SEModAPIInternal.API.Entity.Sector.SectorObject.CubeGrid.CubeBlock.BatteryBlockEntity BatteryBlockEntity =
                            new SEModAPIInternal.API.Entity.Sector.SectorObject.CubeGrid.CubeBlock.BatteryBlockEntity(cube,
                              builder, shielditem);

                 

                        if (BatteryBlockEntity.CurrentStoredPower > damage)
                        {
                            BatteryBlockEntity.CurrentStoredPower -= damage;
                            isdef = true;
                            break;
                        }
                        else
                        {
                            if(BatteryBlockEntity.CurrentStoredPower>0 )
                            {
                                BatteryBlockEntity.CurrentStoredPower = 0;
                                isdef = true;
                                break;
                            }
                        }
                    }
                }

                if (isdef == true)
                {
                    return false;
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
