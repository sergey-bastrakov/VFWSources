//#define DBG

using System;
using System.Reflection;
using System.Collections;
using UnityEngine;
using Vexe.Editor.GUIs;
using Vexe.Editor.Types;
using Vexe.Runtime.Extensions;
using Vexe.Runtime.Helpers;
using Vexe.Runtime.Types;
using UnityObject = UnityEngine.Object;

namespace Vexe.Editor.Drawers
{
	public class MethodDrawer
	{
		private EditorMember[] argMembers;
		private MethodCaller<object, object> invoke;
		private object[] argValues;
		private int[] argKeys;
		private string niceName;
		private bool initialized;

        private string comment;

		private object rawTarget;
        private UnityObject unityTarget;
		private int id;
		private BaseGUI gui;
        private bool isCoroutine;

        private BetterPrefs prefs { get { return BetterPrefsEditor.GetEditorInstance(); } }

		private bool foldout
		{
			get { return prefs.Bools.ValueOrDefault(id); }
			set { prefs.Bools[id] = value; }
		}

		public void Initialize(MethodInfo method, object rawTarget, UnityObject unityTarget, int id, BaseGUI gui)
		{
			this.gui = gui;
			this.rawTarget = rawTarget;
            this.unityTarget = unityTarget;
			this.id = id;

			if (initialized) return;
			initialized = true;

            isCoroutine = method.ReturnType == typeof(IEnumerator);

            var commentAttr = method.GetCustomAttribute<CommentAttribute>();
            if (commentAttr != null)
                comment = commentAttr.comment;

			niceName = method.GetNiceName();

            if (niceName.IsPrefix("dbg") || niceName.IsPrefix("Dbg"))
                niceName = niceName.Remove(0, 3);

			invoke	     = method.DelegateForCall();
			var argInfos = method.GetParameters();
			int len      = argInfos.Length;
			argValues    = new object[len];
			argKeys      = new int[len];
			argMembers   = new EditorMember[len];

			for (int iLoop = 0; iLoop < len; iLoop++)
			{
				int i = iLoop;
				var argInfo = argInfos[i];

				argKeys[i] = RuntimeHelper.CombineHashCodes(id, argInfo.ParameterType.Name + argInfo.Name);

				argValues[i] = TryLoad(argInfos[i].ParameterType, argKeys[i]);

                argMembers[i] = EditorMember.WrapGetSet(
                        @get         : () =>  argValues[i],
                        @set         : x => argValues[i] = x,
                        @rawTarget   : rawTarget,
                        @unityTarget : unityTarget,
                        @attributes  : argInfo.GetCustomAttributes(true) as Attribute[],
                        @name        : argInfo.Name,
                        @id          : argKeys[i],
                        @dataType    : argInfo.ParameterType
                    );
			}

#if DBG
			Log("Method drawer init");
#endif
		}

		public bool OnGUI()
		{
            if (comment != null)
                gui.HelpBox(comment);

			bool changed = false;
			if (Header() && argMembers.Length > 0)
			{
				using (gui.Indent())
				{
					for (int i = 0; i < argMembers.Length; i++)
					{
						bool argChange = gui.Member(argMembers[i], false);
						changed |= argChange;
						if (argChange)
							TrySave(argValues[i], argKeys[i]);
					}
				}
			}
			return changed;
		}

		private bool Header()
		{
			using (gui.Horizontal())
			{
				if (gui.Button(niceName, GUIStyles.Mini))
                {
                    var mb = unityTarget as MonoBehaviour;
                    if (isCoroutine && mb != null)
                        mb.StartCoroutine(invoke(rawTarget, argValues) as IEnumerator);
                    else
                        invoke(rawTarget, argValues);
                }

				gui.Space(12f);
				if (argMembers.Length > 0)
				{
					foldout = gui.Foldout(foldout);
					gui.Space(-11.5f);
				}
			}
			return foldout;
		}

		void TrySave(object obj, int key)
		{
			if (obj == null) return;

			var type = obj.GetType();
            if (type.IsEnum || type == typeof(int))
				 prefs.Ints[key] = (int)obj;
			else if (type == typeof(string))
				 prefs.Strings[key] = (string)obj;
			else if (type == typeof(float))
				 prefs.Floats[key] = (float)obj;
			else if (type == typeof(bool))
				 prefs.Bools[key] = (bool)obj;
		}

		object TryLoad(Type type, int key)
		{
            if (type.IsEnum)
            {
                int value = prefs.Ints.ValueOrDefault(key);
                object result = Enum.ToObject(type, value);
                return result;
            }
            if (type == typeof(int))
				return prefs.Ints.ValueOrDefault(key);
			if (type == typeof(string))
				return prefs.Strings.ValueOrDefault(key);
			if (type == typeof(float))
				return prefs.Floats.ValueOrDefault(key);
			if (type == typeof(bool))
				return prefs.Bools.ValueOrDefault(key);
			return type.GetDefaultValue();
		}
	}
}
