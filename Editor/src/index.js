/**
 * @license
 * Copyright 2023 Google LLC
 * SPDX-License-Identifier: Apache-2.0
 */

import $ from 'jquery';

import hljs from 'highlight.js/lib/core';
import javascript from 'highlight.js/lib/languages/javascript';
hljs.registerLanguage('javascript', javascript);
import 'highlight.js/styles/default.css';

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

const lang = window.localStorage?.getItem('lang');
if (lang) {
	$('#select-language').val(lang);
	const locale1 = await import(`blockly/msg/${lang}.js`);
	const locale2 = await import(`./msg/${lang}`);
	Blockly.setLocale(locale1);
	Blockly.setLocale(locale2.msg);
}

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

$('#select-language').on('change', e => {
	const sel = $(e.target);
	window.localStorage?.setItem('lang', sel.val());
	location.reload();
});

const updateOutput = () => {
	codeDiv.html(hljs.highlight(javascriptGenerator.workspaceToCode(ws), { language: 'javascript' }).value);
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
