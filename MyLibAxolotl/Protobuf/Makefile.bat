setlocal
SET PATH=%PATH%;%USERPROFILE%\.nuget\packages\Google.ProtocolBuffers\2.4.1.555\tools
ProtoGen -namespace="Tr.Com.Eimza.LibAxolotl.State" -umbrella_classname="StorageProtos" -nest_classes=true -output_directory="./" LocalStorageProtocol.proto
ProtoGen -namespace="Tr.Com.Eimza.LibAxolotl.Protocol" -umbrella_classname="WhisperProtos" -nest_classes=true -output_directory="./" WhisperTextProtocol.proto