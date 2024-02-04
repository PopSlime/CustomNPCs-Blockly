/**
 * @license
 * Copyright 2023 Google LLC
 * SPDX-License-Identifier: Apache-2.0
 */

import $ from 'jquery';

import * as Blockly from 'blockly';
import { javascriptGenerator } from 'blockly/javascript';
import { save, load } from './serialization';

import { blocks as eventBlocks } from './event-bus-api/blocks';
import { forBlock as eventForBlocks } from './event-bus-api/generator';
import { msg as eventMsg } from './event-bus-api/msg';

import { blocks as cnpcBlocks } from './custom-npcs/blocks.g';
import { forBlock as cnpcForBlocks } from './custom-npcs/generator.g';
import { toolbox as cnpcToolbox } from './custom-npcs/toolbox.g';
import { msg as cnpcMsg } from './custom-npcs/msg.g';

import { blocks as cnpcBlockOverrides, msg as cnpcMsgOverrides } from './custom-npcs/overrides';

import './index.css';

javascriptGenerator.addReservedWords("event");
Blockly.setLocale(eventMsg);
Blockly.setLocale(cnpcMsg);
Blockly.setLocale(cnpcMsgOverrides);

// Register the blocks and generator with Blockly
Blockly.common.defineBlocks(eventBlocks);
Blockly.common.defineBlocks(cnpcBlocks);
Blockly.common.defineBlocks(cnpcBlockOverrides);
Object.assign(javascriptGenerator.forBlock, eventForBlocks);
Object.assign(javascriptGenerator.forBlock, cnpcForBlocks);

const codeDiv = $('#generated-code').children().first();
const blocklyDiv = $('#blockly-div');
const ws = Blockly.inject(blocklyDiv[0], { toolbox: cnpcToolbox });

const tabs = $('#tabs').children();
const tabContainers = $('#tab-container').children('div');
$('#tabs').children().on("click", e => {
	const tab = $(e.target);
	tabs.removeClass('tab-active');
	tab.addClass('tab-active');
	tabContainers.removeClass('tab-active');
	tabContainers.eq(tab.index()).addClass('tab-active');
});

const updateOutput = () => {
	codeDiv.text(javascriptGenerator.workspaceToCode(ws));
};

load(ws);
updateOutput();

ws.addChangeListener(e => {
	if (e.isUiEvent) return;
	save(ws);
});

ws.addChangeListener(e => {
	if (e.isUiEvent || e.type == Blockly.Events.FINISHED_LOADING || ws.isDragging()) {
		return;
	}
	updateOutput();
});
