using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.IO;
using SEModAPIInternal.Support;
using VRageMath;
using VRage.Common.Utils;
using SEModAPIInternal.API.Common;
using System.Reflection;

namespace ShipShieldPlugin.Utility
{
	public static class Logging
	{
		private static object m_lockObj = new object();
		private static StringBuilder m_sb = new StringBuilder();
		public static void WriteLineAndConsole(string text)
		{
			LogManager.APILog.WriteLineAndConsole(text);
		}

		public static void WriteLineAndConsole(string name, string text)
		{
			lock(m_lockObj)
			{
				m_sb.Clear();
				AppendDateAndTime(m_sb);
				m_sb.Append(" - ");
				m_sb.Append(text);

				string logPath = MyFileSystem.UserDataPath + "\\Logs\\";
				if (!Directory.Exists(logPath))
					Directory.CreateDirectory(logPath);

				File.AppendAllText(logPath + name + ".log", m_sb.ToString() + "\r\n");

                //if (!PluginSettings.Instance.DynamicShowMessages)
                //    return;

				Console.WriteLine(m_sb.ToString());
				m_sb.Clear();
			}			
		}

		private static int GetThreadId()
		{
			return Thread.CurrentThread.ManagedThreadId;
		}

		private static void AppendDateAndTime(StringBuilder sb)
		{
			try
			{
				DateTimeOffset now = DateTimeOffset.Now;
				StringBuilderExtensions.Concat(sb, now.Year, 4U, '0', 10U, false).Append('-');
				StringBuilderExtensions.Concat(sb, now.Month, 2U).Append('-');
				StringBuilderExtensions.Concat(sb, now.Day, 2U).Append(' ');
				StringBuilderExtensions.Concat(sb, now.Hour, 2U).Append(':');
				StringBuilderExtensions.Concat(sb, now.Minute, 2U).Append(':');
				StringBuilderExtensions.Concat(sb, now.Second, 2U).Append('.');
				StringBuilderExtensions.Concat(sb, now.Millisecond, 3U);
			}
			catch (Exception ex)
			{
				Console.WriteLine(ex.ToString());
			}
		}

		private static void AppendThreadInfo(StringBuilder sb)
		{
			sb.Append("Thread: " + GetThreadId().ToString());
		}
	}

	public static class General
	{
		public static string TimeSpanToString(TimeSpan ts)
		{
			if (ts.Days > 0)
				return String.Format("{0}d:{1:D2}h:{2:D2}m:{3:D2}s", ts.Days, ts.Hours, ts.Minutes, ts.Seconds);
			else if (ts.Hours > 0)
				return String.Format("{0:D2}h:{1:D2}m:{2:D2}s", ts.Hours, ts.Minutes, ts.Seconds);
			else
				return String.Format("{0:D2}m:{1:D2}s", ts.Minutes, ts.Seconds);
		}

		public static string Vector3DToString(Vector3D vector)
		{
			return string.Format("({0:F2}, {1:F2}, {2:F2})", vector.X, vector.Y, vector.Z);
		}

		public static bool InheritsOrImplements(this Type child, Type parent)
		{
			parent = ResolveGenericTypeDefinition(parent);

			var currentChild = child.IsGenericType
								   ? child.GetGenericTypeDefinition()
								   : child;

			while (currentChild != typeof(object))
			{
				if (parent == currentChild || HasAnyInterfaces(parent, currentChild))
					return true;

				currentChild = currentChild.BaseType != null
							   && currentChild.BaseType.IsGenericType
								   ? currentChild.BaseType.GetGenericTypeDefinition()
								   : currentChild.BaseType;

				if (currentChild == null)
					return false;
			}
			return false;
		}

		private static bool HasAnyInterfaces(Type parent, Type child)
		{
			return child.GetInterfaces()
				.Any(childInterface =>
				{
					var currentInterface = childInterface.IsGenericType
						? childInterface.GetGenericTypeDefinition()
						: childInterface;

					return currentInterface == parent;
				});
		}

		private static Type ResolveGenericTypeDefinition(Type parent)
		{
			var shouldUseGenericType = true;
			if (parent.IsGenericType && parent.GetGenericTypeDefinition() != parent)
				shouldUseGenericType = false;

			if (parent.IsGenericType && shouldUseGenericType)
				parent = parent.GetGenericTypeDefinition();
			return parent;
		}

		public static T[] RemoveAt<T>(this T[] source, int index)
		{
			T[] dest = new T[source.Length - 1];
			if (index > 0)
				Array.Copy(source, 0, dest, 0, index);

			if (index < source.Length - 1)
				Array.Copy(source, index + 1, dest, index, source.Length - index - 1);

			return dest;
		}
		public static string[] SplitString(string data)
		{
			var result = data.Split('"').Select((element, index) => index % 2 == 0  // If even index
												 ? element.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries)  // Split the item
												 : new string[] { element })  // Keep the entire item					
												 .SelectMany(element => element).ToList();

			return result.ToArray();
		}
	}



    public static class Help
    {
        internal static Object InvokeEntityMethod(Object gameEntity, string methodName)
        {
            return InvokeEntityMethod(gameEntity, methodName, new object[] { });
        }

        internal static Object InvokeEntityMethod(Object gameEntity, string methodName, Object[] parameters)
        {
            return InvokeEntityMethod(gameEntity, methodName, parameters, null);
        }

        internal static Object InvokeEntityMethod(Object gameEntity, string methodName, Object[] parameters, Type[] argTypes)
        {
            try
            {
                MethodInfo method = GetEntityMethod(gameEntity, methodName, argTypes);
                if (method == null)
                    throw new Exception("Method is empty");
                Object result = method.Invoke(gameEntity, parameters);

                return result;
            }
            catch (Exception ex)
            {
                LogManager.APILog.WriteLine("Failed to invoke entity method '" + methodName + "' on type '" + gameEntity.GetType().FullName + "': " + ex.Message);

                if (SandboxGameAssemblyWrapper.IsDebugging)
                    LogManager.ErrorLog.WriteLine(Environment.StackTrace);

                LogManager.ErrorLog.WriteLine(ex);
                return null;
            }
        }

        internal static MethodInfo GetEntityMethod(Object gameEntity, string methodName)
        {
            try
            {
                if (gameEntity == null)
                    throw new Exception("Game entity was null");
                if (methodName == null || methodName.Length == 0)
                    throw new Exception("Method name was empty");
                MethodInfo method = gameEntity.GetType().GetMethod(methodName);
                if (method == null)
                {
                    //Recurse up through the class heirarchy to try to find the method
                    Type type = gameEntity.GetType();
                    while (type != typeof(Object))
                    {
                        method = type.GetMethod(methodName, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static | BindingFlags.FlattenHierarchy);
                        if (method != null)
                            break;

                        type = type.BaseType;
                    }
                }
                if (method == null)
                    throw new Exception("Method not found");
                return method;
            }
            catch (Exception ex)
            {
                LogManager.APILog.WriteLine("Failed to get entity method '" + methodName + "': " + ex.Message);
                if (SandboxGameAssemblyWrapper.IsDebugging)
                    LogManager.ErrorLog.WriteLine(Environment.StackTrace);
                LogManager.ErrorLog.WriteLine(ex);
                return null;
            }
        }

        internal static MethodInfo GetEntityMethod(Object gameEntity, string methodName, Type[] argTypes)
        {
            try
            {
                if (argTypes == null || argTypes.Length == 0)
                    return GetEntityMethod(gameEntity, methodName);

                if (gameEntity == null)
                    throw new Exception("Game entity was null");
                if (methodName == null || methodName.Length == 0)
                    throw new Exception("Method name was empty");
                MethodInfo method = gameEntity.GetType().GetMethod(methodName, argTypes);
                if (method == null)
                {
                    //Recurse up through the class heirarchy to try to find the method
                    Type type = gameEntity.GetType();
                    while (type != typeof(Object))
                    {
                        method = type.GetMethod(methodName, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static | BindingFlags.FlattenHierarchy, Type.DefaultBinder, argTypes, null);
                        if (method != null)
                            break;

                        type = type.BaseType;
                    }
                }
                if (method == null)
                    throw new Exception("Method not found");
                return method;
            }
            catch (Exception ex)
            {
                LogManager.APILog.WriteLine("Failed to get entity method '" + methodName + "': " + ex.Message);
                if (SandboxGameAssemblyWrapper.IsDebugging)
                    LogManager.ErrorLog.WriteLine(Environment.StackTrace);
                LogManager.ErrorLog.WriteLine(ex);
                return null;
            }
        }
    }
}
