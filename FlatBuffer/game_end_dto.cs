// <auto-generated>
//  automatically generated by the FlatBuffers compiler, do not modify
// </auto-generated>

namespace FastBurgerMaker_GameServer
{

using global::System;
using global::System.Collections.Generic;
using global::FlatBuffers;

public struct game_end_dto : IFlatbufferObject
{
  private Table __p;
  public ByteBuffer ByteBuffer { get { return __p.bb; } }
  public static void ValidateVersion() { FlatBufferConstants.FLATBUFFERS_2_0_8(); }
  public static game_end_dto GetRootAsgame_end_dto(ByteBuffer _bb) { return GetRootAsgame_end_dto(_bb, new game_end_dto()); }
  public static game_end_dto GetRootAsgame_end_dto(ByteBuffer _bb, game_end_dto obj) { return (obj.__assign(_bb.GetInt(_bb.Position) + _bb.Position, _bb)); }
  public void __init(int _i, ByteBuffer _bb) { __p = new Table(_i, _bb); }
  public game_end_dto __assign(int _i, ByteBuffer _bb) { __init(_i, _bb); return this; }

  public string Rank(int j) { int o = __p.__offset(4); return o != 0 ? __p.__string(__p.__vector(o) + j * 4) : null; }
  public int RankLength { get { int o = __p.__offset(4); return o != 0 ? __p.__vector_len(o) : 0; } }

  public static Offset<FastBurgerMaker_GameServer.game_end_dto> Creategame_end_dto(FlatBufferBuilder builder,
      VectorOffset rankOffset = default(VectorOffset)) {
    builder.StartTable(1);
    game_end_dto.AddRank(builder, rankOffset);
    return game_end_dto.Endgame_end_dto(builder);
  }

  public static void Startgame_end_dto(FlatBufferBuilder builder) { builder.StartTable(1); }
  public static void AddRank(FlatBufferBuilder builder, VectorOffset rankOffset) { builder.AddOffset(0, rankOffset.Value, 0); }
  public static VectorOffset CreateRankVector(FlatBufferBuilder builder, StringOffset[] data) { builder.StartVector(4, data.Length, 4); for (int i = data.Length - 1; i >= 0; i--) builder.AddOffset(data[i].Value); return builder.EndVector(); }
  public static VectorOffset CreateRankVectorBlock(FlatBufferBuilder builder, StringOffset[] data) { builder.StartVector(4, data.Length, 4); builder.Add(data); return builder.EndVector(); }
  public static VectorOffset CreateRankVectorBlock(FlatBufferBuilder builder, ArraySegment<StringOffset> data) { builder.StartVector(4, data.Count, 4); builder.Add(data); return builder.EndVector(); }
  public static VectorOffset CreateRankVectorBlock(FlatBufferBuilder builder, IntPtr dataPtr, int sizeInBytes) { builder.StartVector(1, sizeInBytes, 1); builder.Add<StringOffset>(dataPtr, sizeInBytes); return builder.EndVector(); }
  public static void StartRankVector(FlatBufferBuilder builder, int numElems) { builder.StartVector(4, numElems, 4); }
  public static Offset<FastBurgerMaker_GameServer.game_end_dto> Endgame_end_dto(FlatBufferBuilder builder) {
    int o = builder.EndTable();
    return new Offset<FastBurgerMaker_GameServer.game_end_dto>(o);
  }
}


}
