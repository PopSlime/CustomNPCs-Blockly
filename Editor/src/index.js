/**
 * @license
 * Copyright 2023 Google LLC
 * SPDX-License-Identifier: Apache-2.0
 */

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

import './index.css';

javascriptGenerator.addReservedWords("event");
Blockly.setLocale(eventMsg);
Blockly.setLocale(cnpcMsg);

// Register the blocks and generator with Blockly
Blockly.common.defineBlocks(eventBlocks);
Blockly.common.defineBlocks(cnpcBlocks);
Object.assign(javascriptGenerator.forBlock, eventForBlocks);
Object.assign(javascriptGenerator.forBlock, cnpcForBlocks);

// Set up UI elements and inject Blockly
const codeDiv = document.getElementById('generatedCode').firstChild;
const outputDiv = document.getElementById('output');
const blocklyDiv = document.getElementById('blocklyDiv');
const ws = Blockly.inject(blocklyDiv, { toolbox: cnpcToolbox });

// This function resets the code and output divs, shows the
// generated code from the workspace, and evals the code.
// In a real application, you probably shouldn't use `eval`.
const runCode = () => {
	const code = javascriptGenerator.workspaceToCode(ws);
	codeDiv.innerText = code;

	outputDiv.innerHTML = '';
};

// Load the initial state from storage and run the code.
load(ws);
runCode();

// Every time the workspace changes state, save the changes to storage.
ws.addChangeListener((e) => {
	// UI events are things like scrolling, zooming, etc.
	// No need to save after one of these.
	if (e.isUiEvent) return;
	save(ws);
});


// Whenever the workspace changes meaningfully, run the code again.
ws.addChangeListener((e) => {
	// Don't run the code when the workspace finishes loading; we're
	// already running it once when the application starts.
	// Don't run the code during drags; we might have invalid state.
	if (e.isUiEvent || e.type == Blockly.Events.FINISHED_LOADING ||
		ws.isDragging()) {
		return;
	}
	runCode();
});
