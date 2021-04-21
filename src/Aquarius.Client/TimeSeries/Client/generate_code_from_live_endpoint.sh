#!/bin/bash

# This is can be installed in your ~/bin folder, outside of any repo.

# Helper functions
exit_abort () {
	[ ! -z "$1" ] && echo ERROR: "$1"
	echo
	echo 'ABORTED!'
	exit $ERRCODE
}

show_usage() {
	echo usage: `basename $0` "EndPointNamespace EndPointRelativeUrl [ServerName (default: localhost)] [OutputPath (default: .)]"
	echo
	echo "Generates an Aquarius NG client service model code file via ServiceStack's built-in /types/<language> generator available on a live REST endpoint"
	echo
	exit_abort "$@"
}

EndPointName=$1
EndPoint=$2
ServerName=$3
OutputPath=$4

[ ! -z "$EndPointName" ] || show_usage "No namespace! Specify a namespace for the generated code"
[ ! -z "$EndPoint" ] || show_usage "No endpoint! Specify a relative URL for the endpoint to inspect"
[ ! -z "$ServerName" ] || ServerName=localhost
[ ! -z "$OutputPath" ] || OutputPath=.

GlobalNamespace=Aquarius.TimeSeries.Client.ServiceModels.$EndPointName

mkdir -p "$OutputPath" || exit_abort "Can't create OutputPath=$OutputPath"
echo "Determining AQUARIUS Server version ..."
ApiVersionJson=`curl -s http://$ServerName/AQUARIUS/apps/v1/version` || exit_abort "Can't determine AQUARIUS server version of $ServerName"
ApiVersion=`echo "$ApiVersionJson" | sed -e "s/{\"ApiVersion\":\"//" -e "s/\"}//"`

echo "Generating $OutputFile ..."
OutputFile=$OutputPath/$EndPointName.cs
curl -s -o "$OutputFile" "http://$ServerName/AQUARIUS/$EndPoint/types/csharp?MakePartial=false&MakeVirtual=false&ExportValueTypes=true&GlobalNamespace=$GlobalNamespace&DefaultNamespaces=System,System.Collections.Generic,ServiceStack,ServiceStack.DataAnnotations,ServiceStack.Web,NodaTime" || exit_abort "Can't read endpoint"

# 2021.1 added a hidden IFileUploadRequest.IsFileRequired property with [ApiMember(ExcludeInSchema = true)][IgnoreDataMember] attributes.
# The built-in ServiceStack code generator endpoint doesn't respect these attributes.
# So we need to manually detect and remove the IsFileRequired property so that everything compiles.
# This blunt-hammer approach works for 2021.1, but may need to be revisited if a property of the same name ever needs to exist in the API
sed -i.bak -e "s/        bool IsFileRequired { get; set; }/        \\/\\/ HACK from generate_code_from_live_endpoint.sh \\/\\/ bool IsFileRequired { get; set; }/" "$OutputFile"

# Append the generated version to the code
echo "namespace $GlobalNamespace" >> "$OutputFile"
echo "{" >> "$OutputFile"
echo "    public static class Current" >> "$OutputFile"
echo "    {" >> "$OutputFile"
echo "        public static readonly AquariusServerVersion Version = AquariusServerVersion.Create(\"$ApiVersion\");" >> "$OutputFile"
echo "    }" >> "$OutputFile"
echo "}" >> "$OutputFile"
unix2dos "$OutputFile"

echo "Complete."
