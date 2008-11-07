#----------------------------------------------------------------
# Copyright (c) Microsoft Corporation.  All rights reserved.
#----------------------------------------------------------------

import sys
import System
import Microsoft
import Common

@Metadata.ImportSingleValue('{Microsoft.Intellipad}Core')
def Initialize(value):
    global Core
    Core = value
    Common.Initialize(Core)

@Metadata.CommandExecuted('{Microsoft.Intellipad}BufferView', '{Microsoft.Intellipad}SetToDoMode', 'Ctrl+Shift+G')
def SetToDoMode(target, sender, args):
    sender.Mode = Core.ComponentDomain.GetBoundValue[System.Object]('{Microsoft.Intellipad}ToDoMode')
    
