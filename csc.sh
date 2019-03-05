#! /bin/bash

references=""
references+="/reference:MySQL.Data.EntityFrameworkCore.Design.dll ";
references+="/reference:MySql.Data.EntityFramework.dll "
references+="/reference:MySql.Data.EntityFramework.dll " 
references+="/reference:MySql.Web.dll " 
references+="/reference:Google.Protobuf.dll "
references+="/reference:MySQL.Data.EntityFrameworkCore.dll "
references+="/reference:MySql.Data.dll "

rm -f ${1}".exe";
csc $references *.cs "/main:${1}";
