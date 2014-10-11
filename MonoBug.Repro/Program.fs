open System
open System.Reflection
open System.Reflection.Emit

// create a new dynamic assembly

let an = new AssemblyName("dynamicAssembly")
let ab = AppDomain.CurrentDomain.DefineDynamicAssembly(an, AssemblyBuilderAccess.Run)
let mb = ab.DefineDynamicModule("mainModule.dll")

// define a generic class Foo<T> with public field T value

let foob = mb.DefineType("Foo", TypeAttributes.Public)
let t = foob.DefineGenericParameters([| "T" |]).[0]
let fb = foob.DefineField("value", t, FieldAttributes.Public)

let foo = foob.CreateType()
let f = foo.GetField("value")

// define a static class Bar with generic method Bar.Get : Foo<'a> -> 'a

let barb = mb.DefineType("Bar", TypeAttributes.Public)

let getB = barb.DefineMethod("Get", MethodAttributes.Public ||| MethodAttributes.Static)
let a = getB.DefineGenericParameters([| "a" |]).[0]
getB.SetReturnType a
let fooT = foo.MakeGenericType [| a :> Type |]
getB.SetParameters([| fooT |])

// emit method body

let ilGen = getB.GetILGenerator()

ilGen.Emit OpCodes.Ldarg_0
ilGen.Emit(OpCodes.Ldfld, TypeBuilder.GetField(fooT, f))
ilGen.Emit OpCodes.Ret

let bar = barb.CreateType()

// emission complete

// parse opcodes using Mono.Reflection

open Mono.Reflection

let m = bar.GetMethod("Get")
let instructions = m.GetInstructions()

let field = 
    instructions 
    |> Seq.pick(fun i -> match i.Operand with :? FieldInfo as f -> Some f | _ -> None)


printfn "%O" <| field.ToString() // "a value"

printfn "%d" field.MetadataToken // crashes in mono  